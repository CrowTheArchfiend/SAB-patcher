# SAB-patcher
Teeny Tiny C# app that breaks StartAllBackX64.dll, giving you unlimited Free Trial days.
Verified supported versions:
1. 3.9.21
2. 3.9.20
3. 3.9.16
   
## How to use
1. Run the `SAB-patcher.exe` (preferably as Administrator).
2. (Optional) Pass in an argument by launching via cmd or powershell.
3. Done.

To verify that it completed successfully, open StartAllBack after _the patcher tells you that it has done its job.._ and go to the "About tab". In there, check if the number of `Trial days left:..` is in the negatives (this only applies if your Free Trial ran out! otherwise its TrustMeBro™ to what the patcher tells you until the trial runs out). You may also restart your computer (can sometimes help with the program loading the DLL).
Example (in my case):

<img width="768" height="321" alt="{8A591371-6F9E-4B80-9E7B-6DB9A82D4C8A}" src="https://github.com/user-attachments/assets/b4d7fee2-ba00-4c96-bb4c-9c40f3fc5346" />

## Arguments
The program comes with 2 arguemnts you can pass when opening via commandline:
  - (no argument) - Finds and patches the DLL (no arguments).         
  - `--test`      - Finds and patches the DLL in a set of "test directories". (you can find them in the source)
  - `--restore`   - Finds and restored the original DLL.

## What does it actually do
It is set to find `StartAllBackX64.dll` in `C:\Program Files\StartAllBack`, `C:\...\AppData\Local\StartAllBack`, or whatever directory the program is launched from (unless you're using `--test`). 
When the DLL is found:
 1. Makes a backup with a `.bak` extension (for `--restore`).
 2. Looks for a sequence of bytes and replaces the first 3 with `0x31, 0xC0, 0xC3`. (you can find what the bytes are in the `Project.cs` file near the top)
 3. Exits.
    _It does some other stuff like kill/restarting processes that are preventing it from editing the `StartAllBackX64.dll` so that it actually works.._

## Troubleshooting
Last time **I** tested the app was on 21/01/2026 on **StartAllBack version 3.9.20** and **3.9.16**.
If the app tells you that it couldn't find the `StartAllBackX64.dll`, put the `SAB Patcher.exe` in the same folder as the DLL.
If the app tells you that it couldn't edit the file (for some reason - usually because it's being used by another process), try using [File Locksmith](https://www.edtittel.com/blog/powertoys-file-locksmith-works-well.html) (or [Powertoys](https://apps.microsoft.com/detail/XP89DCGQ3K6VLD?hl=en-US&gl=US&ocid=pdpshare)) to kill the process that is causing the issues and **restart the patcher**.

## ⚠️ Disclaimer
This tool is for educational and personal use only. Modifying binaries may 
violate the Terms of Service of the software being patched. Use this at your 
own risk. The author is not responsible for any system instability or 
data loss.
