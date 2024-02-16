using System.Text;
using SoulsFormats;

namespace MSBEntityIDChanger;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Run();
    }

    public int FindBytes(byte[] src, byte[] find)
    {
        int index = -1;
        var matchIndex = 0;
        for (var i = 0; i < src.Length; i++)
        {
            if (src[i] == find[matchIndex])
            {
                if (matchIndex == find.Length - 1)
                {
                    index = i - matchIndex;
                    break;
                }
                matchIndex++;
            }
            else if (src[i] == find[0])
            {
                matchIndex = 1;
            }
            else
            {
                matchIndex = 0;
            }
        }
        return index;
    }

    private void Run()
    {
        var msbsDialog = new FolderBrowserDialog();
        if (msbsDialog.ShowDialog() != DialogResult.OK) return;
        var emevdsDialog = new FolderBrowserDialog();
        if (emevdsDialog.ShowDialog() != DialogResult.OK) return;
        var newEventTemplateDialog = new OpenFileDialog();
        if (newEventTemplateDialog.ShowDialog() != DialogResult.OK) return;
        string newEventTemplate = File.ReadAllText(newEventTemplateDialog.FileName);
        const int templateEventId = 9005995;
        const string eventZeroSignature = "Event(0, Default, function() {";
        var eventSlotId = 0;
        var newEntityIdPrefix = 900;
        var newEntityIdSuffix = 0;
        foreach (string msbFile in Directory.EnumerateFiles(msbsDialog.SelectedPath, "*.msb.dcx", SearchOption.AllDirectories))
        {
            MSBE msb = MSBE.Read(DCX.Decompress(File.ReadAllBytes(msbFile)));
            foreach (MSBE.Part.Enemy enemy in msb.Parts.Enemies.Where(enemy => enemy.EntityID == 0))
            {
                if (newEntityIdSuffix == 3999)
                {
                    newEntityIdPrefix++;
                    newEntityIdSuffix = 0;
                }
                enemy.EntityID = int.Parse($"{newEntityIdPrefix}{newEntityIdSuffix.ToString().PadLeft(4, '0')}");
                newEntityIdSuffix++;
            }
            msb.Write($"{Directory.GetParent(msbFile)}/{Path.GetFileNameWithoutExtension(Path.GetFileName(msbFile))}.dcx", DCX.Type.DCX_KRAK);
            var emevdFilePath = $"{emevdsDialog.SelectedPath}\\{Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(msbFile))}.emevd.dcx.js";
            if (!File.Exists(emevdFilePath)) continue;
            List<byte> emevdBytes = File.ReadAllBytes(emevdFilePath).ToList();
            int eventZeroSigIndex = FindBytes(emevdBytes.ToArray(), Encoding.UTF8.GetBytes(eventZeroSignature));
            if (eventZeroSigIndex != -1)
            {
                eventZeroSigIndex += eventZeroSignature.Length;
                emevdBytes.AddRange(Encoding.UTF8.GetBytes(newEventTemplate));
                foreach (MSBE.Part.Enemy enemy in msb.Parts.Enemies)
                {
                    emevdBytes.InsertRange(eventZeroSigIndex, Encoding.UTF8.GetBytes($"\n\tInitializeEvent({eventSlotId}, {templateEventId}, {enemy.EntityID});"));
                    eventSlotId++;
                }
            }
            File.WriteAllBytes(emevdFilePath, emevdBytes.ToArray());
        }
    }
}