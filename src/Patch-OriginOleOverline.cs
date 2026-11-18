using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

internal static class OriginOleOverlinePatch
{
    private const string OriginalHash =
        "E2B19C4E73DFAC30A62FE73B822365746198EA8289AA28B60BDD3BD4D1C62C3B";
    private const string PatchedHash =
        "322E09EF6CF5FF2F1C929839A73BCB0B67510B574A3AA10E1C4EE531AD72E7E3";
    private const int InstructionOffset = 0x27643;
    private static readonly byte[] ForceOverline = { 0x0f, 0xba, 0xee, 0x0c };
    private static readonly byte[] Replacement = { 0x90, 0x90, 0x90, 0x90 };

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: Patch-OriginOleOverline.exe status|apply|restore <Origin installation folder>");
            return 2;
        }

        var action = args[0].ToLowerInvariant();
        if (action != "status" && action != "apply" && action != "restore")
        {
            Console.Error.WriteLine("Expected status, apply, or restore.");
            return 2;
        }

        try
        {
            var directory = Path.GetFullPath(args[1]);
            var library = Path.Combine(directory, "otext.dll");
            if (!File.Exists(Path.Combine(directory, "Origin64.exe")) || !File.Exists(library))
                throw new IOException("Select the Origin installation folder containing Origin64.exe and otext.dll.");

            using (var mutex = new Mutex(false, @"Local\OriginOleOverlinePatch2024"))
            {
                bool locked;
                try { locked = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { locked = true; }
                if (!locked)
                    throw new IOException("Another Origin patch operation is already running.");
                try { return Run(action, library); }
                finally { mutex.ReleaseMutex(); }
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static int Run(string action, string library)
    {
        var backup = library + ".before-origin-ole-overline-patch";
        var hash = HashFile(library);
        var original = hash == OriginalHash;
        var patched = hash == PatchedHash;
        if (action == "status")
        {
            Console.WriteLine("{0}: {1}", original ? "Original" : patched ? "Patched" : "Unsupported", library);
            Console.WriteLine("SHA-256: {0}", hash);
            Console.WriteLine("Verified original backup: {0}", File.Exists(backup) && HashFile(backup) == OriginalHash);
            return original || patched ? 0 : 1;
        }

        if (!original && !patched)
            throw new IOException("Unsupported otext.dll SHA-256: " + hash);
        if (File.Exists(backup) && HashFile(backup) != OriginalHash)
            throw new IOException("The backup does not match the supported original DLL: " + backup);

        var apply = action == "apply";
        if (apply && patched)
        {
            Console.WriteLine("Patch already applied: {0}", library);
            return 0;
        }
        if (!apply && original)
        {
            Console.WriteLine("Original DLL is already in place: {0}", library);
            return 0;
        }

        var staging = library + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var expected = apply ? PatchedHash : OriginalHash;
        try
        {
            if (apply)
            {
                var bytes = File.ReadAllBytes(library);
                for (int i = 0; i < ForceOverline.Length; ++i)
                    if (bytes[InstructionOffset + i] != ForceOverline[i])
                        throw new IOException("Unexpected instruction at the patch offset.");

                // Remove only the instruction that forces overline. The text run's own flags remain intact.
                Buffer.BlockCopy(Replacement, 0, bytes, InstructionOffset, Replacement.Length);
                File.WriteAllBytes(staging, bytes);
                if (!File.Exists(backup))
                    File.Copy(library, backup, false);
            }
            else
            {
                if (!File.Exists(backup))
                    throw new IOException("Original backup is missing: " + backup);
                File.Copy(backup, staging, false);
            }

            if (HashFile(staging) != expected || HashFile(library) != hash || HashFile(backup) != OriginalHash)
                throw new IOException("The staged DLL, installed DLL, or backup changed during preparation.");

            try { File.Replace(staging, library, null); }
            catch (Exception error)
            {
                throw new IOException("Cannot replace otext.dll. Check write access and close Origin if it is using this installation. " + error.Message, error);
            }

            if (HashFile(library) != expected)
                throw new IOException("Could not verify the installed DLL. The original backup is at " + backup);
            Console.WriteLine("{0} succeeded: {1}", action, library);
            return 0;
        }
        finally
        {
            if (File.Exists(staging))
                File.Delete(staging);
        }
    }

    private static string HashFile(string path)
    {
        using (var data = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
    }
}
