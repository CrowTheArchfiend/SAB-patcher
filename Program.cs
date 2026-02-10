using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

// cfg
string targetDll = "StartAllBackX64.dll";
byte[] ahead = { 0xC3, 0xCC, 0xCC, 0xCC };
byte[] signature = { 0x48, 0x89, 0x5C, 0x24, 0x18, 0x57, 0x48, 0x83, 0xEC, 0x30, 0x48, 0x8D, 0x4C, 0x24, 0x48 };
byte[] patch = { 0x31, 0xC0, 0xC3 };

// console colors
string r = "\x1b[31m", g = "\x1b[32m", b = "\x1b[34m", y = "\x1b[33m", c = "\x1b[0m"; // c is Clear formatting

string[] standardPaths = {
    AppDomain.CurrentDomain.BaseDirectory,
    @"C:\Program Files\StartAllBack",
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StartAllBack")
};
string[] testPaths = { @"C:\Users\Public\Downloads\TestFolder", @"D:\Debug\Libs" };

// args
bool isRestore = args.Contains("--restore");
bool isTestMode = args.Contains("--test");

bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

EnableAnsiColors();
Console.WriteLine($"{b}===== Sig-based SAB patcher ====={c}");
Console.WriteLine($"{b}https://github.com/CrowTheArchfiend/SAB-patcher{c}");
if (!isAdmin) Console.WriteLine($"{y}[!] Warning: Not running as Administrator. Patching may fail.{c}");
Console.WriteLine($"Mode:             {(isRestore ? "RESTORE" : (isTestMode ? "TEST-PATCH" : "PATCH"))}");
Console.WriteLine("Last Tested:      10/02/2026");
Console.WriteLine("on SAB version:   3.9.21");


string[] searchPaths = isTestMode ? testPaths : standardPaths;
string? foundPath = searchPaths.Select(p => Path.Combine(p, targetDll)).FirstOrDefault(File.Exists);

// exec
if (foundPath == null)
{
    Console.WriteLine($"{r}Error:{c} Target DLL not found.");
}
else
{
    try
    {
        if (isRestore) UnlockAndExecute(foundPath, () => DllRestore(foundPath));
        else UnlockAndExecute(foundPath, () => DllPatch(foundPath));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{r}FAILED:{c} {ex.Message}");
    }
}

WaitForExit();

void DllPatch(string filePath)
{
    try
    {
        string backupPath = filePath + ".bak";

        if (!File.Exists(backupPath))
        {
            File.Copy(filePath, backupPath);
            Console.WriteLine($"{g}[Backup]{c} Original file backed up to .bak");
        }
        else
        {
            Console.WriteLine($"{y}[Skip]{c} Backup already exists. Keeping original.");
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        int patchIndex = -1;

        // Scan the file for the combined pattern
        for (int i = 0; i <= fileData.Length - (ahead.Length + signature.Length); i++)
        {
            // Check if 'ahead' matches at current position
            if (fileData.AsSpan(i, ahead.Length).SequenceEqual(ahead))
            {
                // Check if 'signature' follows immediately after
                if (fileData.AsSpan(i + ahead.Length, signature.Length).SequenceEqual(signature))
                {
                    patchIndex = i + ahead.Length;
                    break; // Found
                }
            }
        }

        if (patchIndex == -1) { Console.WriteLine($"{r}Error:{c} Signature not found. (Wrong version or already patched)"); return; }

        // Apply patch at signature (-ahead location)
        for (int i = 0; i < patch.Length; i++)
            fileData[patchIndex + i] = patch[i];

        File.WriteAllBytes(filePath, fileData);
        int patchEndIndex = patchIndex + patch.Length - 1;
        Console.WriteLine($"{g}Success:{c} Patch applied at 0x{patchIndex:X} through 0x{patchEndIndex:X}.");
    }
    catch (UnauthorizedAccessException) { Console.WriteLine($"{r}Error: Access Denied. Run as Admin!{c}"); }
    catch (Exception ex) { Console.WriteLine($"{r}Failure: {ex.Message}{c}"); }
}

void DllRestore(string filePath)
{
    string backupPath = filePath + ".bak";
    if (!File.Exists(backupPath))
    {
        Console.WriteLine($"{r}Error:{c} Backup file not found.");
        return;
    }
    try
    {
        File.Copy(backupPath, filePath, overwrite: true);
        Console.WriteLine($"{g}Success:{c} Original DLL restored from backup.");
    }
    catch (Exception ex) { Console.WriteLine($"{r}Restore failure: {ex.Message}{c}"); }
}

void UnlockAndExecute(string filePath, Action DllAction)
{
    string sessionKey = Guid.NewGuid().ToString();
    uint handle;
    int res = RmStartSession(out handle, 0, sessionKey);
    if (res != 0) throw new Exception("Could not start Restart Manager session.");

    try
    {
        string[] resources = { filePath };
        RmRegisterResources(handle, (uint)resources.Length, resources, 0, IntPtr.Zero, 0, IntPtr.Zero);

        uint pnProcInfoNeeded = 0;
        uint pnProcInfo = 0;
        uint rebootReasons;

        RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, IntPtr.Zero, out rebootReasons);

        if (pnProcInfoNeeded > 0)
        {
            Console.WriteLine($"{y}[!] File is locked by {pnProcInfoNeeded} process(es):{c}");

            // Mem fix
            pnProcInfo = pnProcInfoNeeded;
            int structSize = Marshal.SizeOf<RM_PROCESS_INFO>();
            IntPtr pInfo = Marshal.AllocHGlobal((int)pnProcInfo * structSize);
            try
            {
                if (RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, pInfo, out rebootReasons) == 0)
                {
                    for (int i = 0; i < pnProcInfo; i++)
                    {
                        var info = Marshal.PtrToStructure<RM_PROCESS_INFO>((IntPtr)((long)pInfo + (i * structSize)));
                        Console.WriteLine($"    -> {r}PID: {info.Process.dwProcessId}{c} | Name: {info.strAppName}");
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pInfo); }

            Console.WriteLine($"{y}\n[!] Forcing closure of locking processes...{c}");

            // fuck you 
            string[] targets = { "explorer.exe", "StartAllBackCfg.exe" };
            foreach (var t in targets)
            {
                using var p = Process.Start(new ProcessStartInfo("taskkill", $"/F /IM {t}") { CreateNoWindow = true, UseShellExecute = false });
                p?.WaitForExit();
            }
            Thread.Sleep(1500); // Windows tea time
        }
        // Are you dead yet?  Are you dead yet?  Are you dead yet? 
        int attempts = 0;
        while (attempts < 10)
        {
            try
            {
                DllAction();
                break; // Success!
            }
            catch (IOException)
            {
                attempts++;
                if (attempts >= 10) throw;
                Console.WriteLine($"{y}[Retry {attempts}]{c} File still busy, waiting...");
                Thread.Sleep(700);
            }
        }
    }
    finally
    {
        RmEndSession(handle);
        if (Process.GetProcessesByName("explorer").Length == 0)
        {
            Console.WriteLine($"{g}[+] Restarting Explorer...{c}");
            Process.Start("explorer.exe");
        }
    }
}

void WaitForExit()
{
    Console.WriteLine($"\n{b}======================================={c}");
    Console.WriteLine("Press any key to close this window...");
    Console.ReadKey(true);
}


[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr GetStdHandle(int nStdHandle);

[DllImport("kernel32.dll")]
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

[DllImport("kernel32.dll")]
static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

const int STD_OUTPUT_HANDLE = -11;
const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

void EnableAnsiColors()
{
    var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
    if (GetConsoleMode(iStdOut, out uint outConsoleMode))
    {
        outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        SetConsoleMode(iStdOut, outConsoleMode);
    }
}

[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
static extern int RmStartSession(out uint pSessionHandle, uint dwSessionFlags, string strSessionKey);

[DllImport("rstrtmgr.dll")]
static extern int RmEndSession(uint dwSessionHandle);

[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
static extern int RmRegisterResources(uint dwSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, IntPtr rgApplications, uint nServices, IntPtr rgsServiceNames);

[DllImport("rstrtmgr.dll")]
static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, IntPtr rgAffectedApps, out uint lpdwRebootReasons);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct RM_PROCESS_INFO
{
    public RM_UNIQUE_PROCESS Process;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string strAppName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string strServiceShortName;
    public RM_APP_TYPE ApplicationType;
    public uint AppStatus;
    public uint TSSessionId;
    [MarshalAs(UnmanagedType.Bool)]
    public bool bRestartable;
}

[StructLayout(LayoutKind.Sequential)]
public struct RM_UNIQUE_PROCESS
{
    public int dwProcessId;
    public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
}

public enum RM_APP_TYPE
{
    RmUnknownApp = 0, RmMainWindow = 1, RmOtherWindow = 2, RmService = 3, RmExplorer = 4, RmConsole = 5, RmCritical = 1000
}

