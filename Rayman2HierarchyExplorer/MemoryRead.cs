﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public class MemoryRead {
  public const int PROCESS_WM_READ = 0x0010;
  public const int PROCESS_VM_WRITE = 0x0020;
  public const int PROCESS_VM_OPERATION = 0x0008;

  [DllImport("kernel32.dll")]
  public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

  [DllImport("kernel32.dll")]
  public static extern bool ReadProcessMemory(int hProcess,
    int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

  [DllImport("kernel32.dll", SetLastError = true)]
  public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress,
  byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
}