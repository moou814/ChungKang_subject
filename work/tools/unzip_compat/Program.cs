using System.IO.Compression;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

if (args.Length < 2)
{
    return Fail("Usage: unzip -Z1 <zip> | unzip -p <zip> <entry>");
}

try
{
    if (args[0] == "-Z1" && args.Length == 2)
    {
        using ZipArchive archive = ZipFile.OpenRead(args[1]);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            Console.WriteLine(entry.FullName);
        }

        return 0;
    }

    if (args[0] == "-p" && args.Length == 3)
    {
        using ZipArchive archive = ZipFile.OpenRead(args[1]);
        ZipArchiveEntry? entry = archive.GetEntry(args[2]);
        if (entry == null)
        {
            return Fail($"Entry not found: {args[2]}");
        }

        await using Stream output = Console.OpenStandardOutput();
        await using Stream input = entry.Open();
        await input.CopyToAsync(output);
        return 0;
    }

    return Fail("Unsupported unzip arguments.");
}
catch (Exception ex)
{
    return Fail(ex.Message);
}
