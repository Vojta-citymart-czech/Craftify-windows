CreateObject("Wscript.Shell").Run """" & CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\bin\Debug\net8.0-windows\CraftifyWPF.exe""", 0, False
