using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Threading;

namespace Rayman2HierarchyExplorer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {


    void OnTimer(object source, EventArgs e) {
      if (autoRefreshEnabled.IsChecked == true) {
        retrieveHierarchy(source, null);
      }
    }

    public MainWindow() {
      InitializeComponent();
      DispatcherTimer timer = new DispatcherTimer();
      timer.Tick += new EventHandler(OnTimer);
      timer.Interval = new TimeSpan(0,0,0,1);
      timer.Start();
    }

    int processHandle;
    int clipboardBuffer = 0;
    Matrix clipboardMatrix;
    Dictionary<int, bool> expandDong = new Dictionary<int, bool>();

    public static string ReadString(int processHandle, int address, int maxLength) {
      
      byte[] bytes = new byte[maxLength];
      int numRead = 0;
      if (!MemoryRead.ReadProcessMemory(processHandle, address, bytes, bytes.Length, ref numRead))
        return "<error>";

      bool encounteredNullByte = false;
      for (int i=0;i<bytes.Length;i++) {
        if (bytes[i] == 0) {
          encounteredNullByte = true;
        }
        if (encounteredNullByte) {
          bytes[i] = 0;
        }
      }

      string result = System.Text.Encoding.Default.GetString(bytes);
      Regex rgx = new Regex("[^a-zA-Z0-9_.]+");
      result = rgx.Replace(result, "");

      return result;
    }

    public static T GetStructure<T>(int processHandle, int address, int structSize = 0) {
      if (structSize == 0)
        structSize = Marshal.SizeOf(typeof(T));
      byte[] bytes = new byte[structSize];
      int numRead = 0;
      if (!MemoryRead.ReadProcessMemory(processHandle, address, bytes, bytes.Length, ref numRead))
        return default(T);
        //throw new Exception("ReadProcessMemory failed");
      //if (numRead != bytes.Length)
        //throw new Exception("Number of bytes read does not match structure size");
      GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
      handle.Free();
      return structure;
    }

    static byte[] getStructureBytes(object str) {
      int size = Marshal.SizeOf(str);
      byte[] arr = new byte[size];
      IntPtr ptr = Marshal.AllocHGlobal(size);

      Marshal.StructureToPtr(str, ptr, true);
      Marshal.Copy(ptr, arr, 0, size);
      Marshal.FreeHGlobal(ptr);

      return arr;
    }

    public static void WriteStructure(int processHandle, int address, object structure, int structSize = 0) {
      byte[] arr = getStructureBytes(structure);

      int numWritten = 0;
      MemoryRead.WriteProcessMemory(processHandle, address, arr, arr.Length, ref numWritten);
      Console.WriteLine("Wrote " + numWritten + " bytes to " + address);
    }

    public static int ReadIntFromMemory(int processHandle, int address) {

      byte[] buffer = new byte[4];
      int readBytes = 0;
      MemoryRead.ReadProcessMemory(processHandle, address, buffer, buffer.Length, ref readBytes);
      if (readBytes!=4) {
        return 0;
      }
      return BitConverter.ToInt32(buffer, 0);
    }

    private void retrieveHierarchyRecursive(SOTreeNode parent, int mainCharacterAddress, int previousMainSectorAddress) {
      int nextBrother = parent.superObject.firstChild; // First brother is the child of the parent object
      if (nextBrother == 0) {
        return;
      }
      do {
        SuperObject so = GetStructure<SuperObject>(processHandle, nextBrother);

        bool isMainChar = (mainCharacterAddress == nextBrother) ? true : false;
        bool isPreviousSector = (previousMainSectorAddress == nextBrother) ? true : false;

        string name = "SO Type " + so.type + " address 0x" + nextBrother.ToString("X");
        SuperObjectInfo soInfo = GetStructure<SuperObjectInfo>(processHandle, so.info);
        int someInfoStruct = soInfo.standardGameStruct;
        int string1address = someInfoStruct + 0x50;
        int string2address = someInfoStruct + 0x84;
        int string3address = someInfoStruct + 0xAC;
        string string1 = ReadString(processHandle, string1address, 32);
        string string2 = ReadString(processHandle, string2address, 32);
        string string3 = ReadString(processHandle, string3address, 32);

        bool visible = SuperObject.getVisibleBit(so.renderBits);
        if (!visible) {
          name = "(INVIS) " + name;
        }

        if (so.type==2)
        name += "(" + string1 + ", " + string2 + ", " + string3 + ")";

        if (isMainChar) {
          if (so.type==2)
            name = "(M) " + name;
          else
            name = "(ACTIVE) " + name;
        }

        if (isPreviousSector) {
          name = "(PREV ACTIVE) " + name;
        }

          var childNode = new SOTreeNode() {
          name = name,
          superObject = so,
          superObjectAddress = nextBrother,
          isMainCharacter = isMainChar
        };

        retrieveHierarchyRecursive(childNode, mainCharacterAddress, previousMainSectorAddress);
        parent.subitems.Add(childNode);
        nextBrother = so.nextBrother;
        
      } while (nextBrother != 0);

    }

    int b = 0;

    private void fillTree(SOTreeNode treeItem, TreeViewItem treeViewItem) {

      foreach(SOTreeNode node in treeItem.subitems) {

        TreeViewItem newTreeViewItem = new TreeViewItem() {
          Header = node.name,
          Tag = node
        };

        ContextMenu menu = new ContextMenu();
        newTreeViewItem.ContextMenu = menu;

        MenuItem m_remove = new MenuItem() { Header = "Remove from hierarchy (iffy)" };
        m_remove.Click += (sender, e) => removeFromHierarchy(node);
        MenuItem m_copyRenderInfo = new MenuItem() { Header = "Copy RenderInfo address" };
        m_copyRenderInfo.Click += (sender, e) => copyRenderInfo(node);
        MenuItem m_pasteRenderInfo = new MenuItem() { Header = "Paste RenderInfo address" };
        m_pasteRenderInfo.Click += (sender, e) => pasteRenderInfo(node);

        MenuItem m_copyMatrix = new MenuItem() { Header = "Copy Transform Matrix" };
        m_copyMatrix.Click += (sender, e) => copyMatrix(node);
        MenuItem m_pasteMatrix = new MenuItem() { Header = "Paste Transform Matrix" };
        m_pasteMatrix.Click += (sender, e) => pasteMatrix(node);

        MenuItem m_setCameraTrue = new MenuItem() {Header = "Set Camera to true"};
        m_setCameraTrue.Click += (sender, e) => setSOCamera(node, true);
        MenuItem m_setCameraFalse = new MenuItem() { Header = "Set Camera to false" };
        m_setCameraFalse.Click += (sender, e) => setSOCamera(node, false);

        MenuItem m_setVisible = new MenuItem() { Header = "Set visible" };
        m_setVisible.Click += (sender, e) => setSOVisible(node, true);
        MenuItem m_setInvisible = new MenuItem() { Header = "Set invisible" };
        m_setInvisible.Click += (sender, e) => setSOVisible(node, false);


        MenuItem m_setVisibleChildren = new MenuItem() { Header = "Set visible (including children)" };
        m_setVisibleChildren.Click += (sender, e) => setAllVisible(newTreeViewItem, true);
        MenuItem m_setInvisibleChildren = new MenuItem() { Header = "Set invisible (including children)" };
        m_setInvisibleChildren.Click += (sender, e) => setAllVisible(newTreeViewItem, false);

        menu.Items.Add(m_remove);
        menu.Items.Add(m_copyMatrix);
        menu.Items.Add(m_pasteMatrix);
        menu.Items.Add(m_copyRenderInfo);
        menu.Items.Add(m_pasteRenderInfo);
        menu.Items.Add(m_setCameraTrue);
        menu.Items.Add(m_setCameraFalse);
        menu.Items.Add(m_setVisible);
        menu.Items.Add(m_setInvisible);
        menu.Items.Add(m_setVisibleChildren);
        menu.Items.Add(m_setInvisibleChildren);

        fillTree(node, newTreeViewItem);

        if (expandDong.ContainsKey(node.superObjectAddress) && expandDong[node.superObjectAddress] == true) {
          Console.WriteLine(treeItem.name);
          newTreeViewItem.ExpandSubtree();
        }
        b += 1;

        treeViewItem.Items.Add(newTreeViewItem);
      }
    }

    private void setSOCamera(SOTreeNode so, bool camera) {
      SuperObjectInfo info = GetStructure<SuperObjectInfo>(processHandle, so.superObject.info);
      info.isCamera = camera ? 1 : 0;
      WriteStructure(processHandle, so.superObject.info, info);
    }

    private void setSOVisible( SOTreeNode soNode, bool visible) {
      SuperObject soStruct = GetStructure<SuperObject>(processHandle, soNode.superObjectAddress);
      soStruct.renderBits = SuperObject.setVisibleBit(soStruct.renderBits, visible);
      WriteStructure(processHandle, soNode.superObjectAddress, soStruct);
    }

    private void copyMatrix(SOTreeNode so) {
      Matrix matrix = GetStructure<Matrix>(processHandle, so.superObject.mainMatrix);
      clipboardMatrix = matrix;
    }

    private void pasteMatrix(SOTreeNode so) {
      WriteStructure(processHandle, so.superObject.mainMatrix, clipboardMatrix);
      WriteStructure(processHandle, so.superObject.otherMatrix, clipboardMatrix);
    }

    private void copyRenderInfo(SOTreeNode so) {
      SuperObjectInfo info = GetStructure<SuperObjectInfo>(processHandle, so.superObject.info);
      clipboardBuffer = info.renderInfo;
    }

    private void pasteRenderInfo(SOTreeNode so) {
      SuperObjectInfo info = GetStructure<SuperObjectInfo>(processHandle, so.superObject.info);
      info.renderInfo = clipboardBuffer;
      WriteStructure(processHandle, so.superObject.info, info);
    }

    private void removeFromHierarchy(SOTreeNode so) {
      so.name = "REMOVED FROM HIERARCHY " + so.name;
      SuperObject prevBrother = GetStructure<SuperObject>(processHandle, so.superObject.prevBrother);
      prevBrother.nextBrother = so.superObject.nextBrother;

      WriteStructure(processHandle, so.superObject.prevBrother, prevBrother);
    }

    private void checkExpandedDongs(TreeViewItem item) {
      var items = item.Items;

      if (item.IsExpanded && !items.IsEmpty) {
        if (item.Tag!=null) {
          SOTreeNode node = (SOTreeNode) item.Tag;
          expandDong.Add(node.superObjectAddress, true);
        }
      }

      foreach (TreeViewItem oldItem in items) {
        checkExpandedDongs(oldItem);
      }
    }

    private void retrieveHierarchy(object sender, RoutedEventArgs e) {

      expandDong.Clear();
      var items = treeView.Items;

      foreach(TreeViewItem oldItem in items) {
        checkExpandedDongs(oldItem);
      }

      items.Clear();

      Process process;
      if (Process.GetProcessesByName("Rayman2").Length > 0) {
        process = Process.GetProcessesByName("Rayman2")[0];
        processHandle = MemoryRead.OpenProcess(MemoryRead.PROCESS_WM_READ | MemoryRead.PROCESS_VM_WRITE | MemoryRead.PROCESS_VM_OPERATION, false, process.Id).ToInt32();

        int dynamicWorldAddress = ReadIntFromMemory(processHandle, 0x500FD0);
        int staticWorldAddress = ReadIntFromMemory(processHandle, 0x500FC0);

        int mainCharacterObjectAddress = ReadIntFromMemory(processHandle, 0x4FF764);

        SuperObject dynamicWorld = GetStructure<SuperObject>(processHandle, dynamicWorldAddress);
        SuperObject staticWorld = GetStructure<SuperObject>(processHandle, staticWorldAddress);

        SOTreeNode dynamicTreeRoot = new SOTreeNode();
        dynamicTreeRoot.name = "Dynamic World Root";
        dynamicTreeRoot.superObject = dynamicWorld;
        retrieveHierarchyRecursive(dynamicTreeRoot, mainCharacterObjectAddress, 0);

        int mainSectorAddress = ReadIntFromMemory(processHandle, 0x500FB0);
        int previousMainSectorAddress = ReadIntFromMemory(processHandle, 0x500FC8);

        SOTreeNode staticTreeRoot = new SOTreeNode();
        staticTreeRoot.name = "Static World Root";
        staticTreeRoot.superObject = staticWorld;
        retrieveHierarchyRecursive(staticTreeRoot, mainSectorAddress, previousMainSectorAddress);

        TreeViewItem dynamicTreeViewItemRoot = new TreeViewItem() {
          Header = "Dynamic World"
        };
        dynamicTreeViewItemRoot.ExpandSubtree();

        TreeViewItem staticTreeViewItemRoot = new TreeViewItem() {
          Header = "Static World"
        };
        staticTreeViewItemRoot.ExpandSubtree();

        b = 0;
        fillTree(dynamicTreeRoot, dynamicTreeViewItemRoot);
        fillTree(staticTreeRoot, staticTreeViewItemRoot);

        items.Add(dynamicTreeViewItemRoot);
        items.Add(staticTreeViewItemRoot);

        Console.WriteLine(process);
      } else {
        Console.WriteLine("Rayman2.exe not found");
      }


      
    }

    private void setAllVisible(TreeViewItem item, bool visible) {
      var items = item.Items;
      
      if (item.Tag != null) {
        SOTreeNode node = (SOTreeNode)item.Tag;
        setSOVisible(node, visible);
      }

      foreach (TreeViewItem oldItem in items) {
        setAllVisible(oldItem, visible);
      }
    }

    private void ButtonMakeAllVisible(object sender, RoutedEventArgs e) {
      setAllVisible((TreeViewItem)treeView.Items.GetItemAt(0), true);
    }

    private void ButtonMakeAllInvisible(object sender, RoutedEventArgs e) {
      setAllVisible((TreeViewItem)treeView.Items.GetItemAt(0), false);
    }
  }
}
