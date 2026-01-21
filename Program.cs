using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;

// cfg
string targetDll = "StartAllBackX64.dll";
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

Console.WriteLine($"{b}-------- Sig-based SAB patcher --------{c}");
if (!isAdmin) Console.WriteLine($"{y}[!] Warning: Not running as Administrator. Patching may fail.{c}");
Console.WriteLine($"Mode:          {(isRestore ? "RESTORE" : (isTestMode ? "TEST-PATCH" : "PATCH"))}");
Console.WriteLine("Last Tested:      21/01/2026");
Console.WriteLine("on SAB version:   3.9.20\n");

string[] searchPaths = isTestMode ? testPaths : standardPaths;
string? foundPath = searchPaths.Select(p => Path.Combine(p, targetDll)).FirstOrDefault(File.Exists);

// exec
if (foundPath == null)
{
    Console.WriteLine($"{r}Error:{c} Target DLL not found.");
}
else
{
    if (isRestore) RestartExplorer(() => DllRestore(foundPath)); else RestartExplorer(() => DllPatch(foundPath));
}

WaitForExit();

void DllPatch(string filePath)
{
    try
    {
        string backupPath = filePath + ".bak";

        // Check if backup already exists to not write over the original dll
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
        int index = fileData.AsSpan().IndexOf(signature);

        if (index == -1)
        {
            Console.WriteLine($"{r}Error:{c} Signature not found. (Already patched or wrong version)");
            return;
        }

        for (int i = 0; i < patch.Length; i++) fileData[index + i] = patch[i];

        File.WriteAllBytes(filePath, fileData);
        Console.WriteLine($"{g}Success:{c} Patch applied at 0x{index:X}.");
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
    } catch (Exception ex) { Console.WriteLine($"{r}Restore failure: {ex.Message}{c}"); }
}

void RestartExplorer(Action DllAction)
{
    Console.WriteLine($"{y}[!] Restarting Explorer to unlock DLL...{c}");
    foreach (var proc in Process.GetProcessesByName("explorer"))
    {
        try { proc.Kill(); proc.WaitForExit(); } catch { }
    }
    Thread.Sleep(500); // Windows moment
    DllAction();
    Console.WriteLine($"{g}[+] Restarting Explorer...{c}");
    Process.Start("explorer.exe");
}

void WaitForExit()
{
    Console.WriteLine($"\n{b}======================================={c}");
    Console.WriteLine("Press any key to close this window...");
    Console.ReadKey(true);
}