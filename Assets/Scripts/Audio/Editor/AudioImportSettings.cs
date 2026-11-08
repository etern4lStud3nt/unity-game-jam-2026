using UnityEditor;
using UnityEngine;

// Gives audio files sensible import settings based on the folder they are dropped into under Assets/Audio:
//   Music/    -> streamed from disk (long files, not kept in memory)
//   Ambience/ -> compressed in memory (medium length, always playing)
//   SFX/, UI/ -> decompressed on load (short, must start instantly)
// Only applies the first time a file is imported, so settings changed by hand afterwards are kept.
public class AudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        string path = assetPath.Replace('\\', '/');
        if (!assetImporter.importSettingsMissing || !path.StartsWith("Assets/Audio/"))
            return;

        var importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;

        if (path.Contains("/Music/"))
        {
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
        }
        else if (path.Contains("/Ambience/"))
        {
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.loadInBackground = true;
        }
        else if (path.Contains("/SFX/") || path.Contains("/UI/"))
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
        }
        else
        {
            return;
        }

        importer.defaultSampleSettings = settings;
    }
}
