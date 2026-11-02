using System.Diagnostics;
using System.IO;
using NAudio.Wave;
namespace CrateScope;

// One decode path for analysis and auditioning; never writes to the source.
public sealed class AudioSource : IDisposable {
 public AudioFileReader Reader {get;}
 readonly string? temporary;
 public const string Filter="Audio|*.wav;*.wave;*.mp3;*.flac;*.aif;*.aiff;*.aifc;*.m4a;*.aac;*.ogg;*.oga;*.opus;*.wma;*.alac;*.ape;*.wv;*.mp2;*.ac3;*.aiff;*.caf;*.au;*.mka;*.mp4|Alle Dateien|*.*";
 public static readonly HashSet<string> Extensions=Filter.Split('|')[1].Split(';').Select(s=>s[1..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
 AudioSource(AudioFileReader reader,string? temp=null){Reader=reader;temporary=temp;}
 public static AudioSource Open(string path) {
  if(!File.Exists(path))throw new FileNotFoundException("Audiodatei nicht gefunden.",path);
  try{return new AudioSource(new AudioFileReader(path));}
  catch(Exception native){
   var exe=FindFfmpeg();if(exe==null)throw new InvalidDataException("Dieser Codec benötigt FFmpeg. ffmpeg.exe neben die Anwendung legen. Native Meldung: "+native.Message);
   string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Harmoniq-"+Guid.NewGuid()+".wav");
   try {
    var start=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
    foreach(var arg in new[]{"-nostdin","-v","error","-protocol_whitelist","file,pipe","-i",System.IO.Path.GetFullPath(path),"-map","0:a:0","-vn","-t","1801","-ac","2","-ar","44100","-c:a","pcm_s16le","-n",temp})start.ArgumentList.Add(arg);
    using var process=Process.Start(start)??throw new IOException("FFmpeg konnte nicht gestartet werden.");var error=process.StandardError.ReadToEndAsync();
    if(!process.WaitForExit(120000)){process.Kill(true);process.WaitForExit();throw new IOException("Dekodierung nach 120 Sekunden abgebrochen.");}
    if(process.ExitCode!=0)throw new InvalidDataException("Audioformat beschädigt oder nicht unterstützt: "+error.GetAwaiter().GetResult());
    return new AudioSource(new AudioFileReader(temp),temp);
   } catch {if(File.Exists(temp))File.Delete(temp);throw;}
  }
 }
 public static string? FindFfmpeg(){var dirs=new[]{AppContext.BaseDirectory}.Concat((Environment.GetEnvironmentVariable("PATH")??"").Split(System.IO.Path.PathSeparator));return dirs.Select(d=>System.IO.Path.Combine(d,"ffmpeg.exe")).FirstOrDefault(File.Exists);}
 public void Dispose(){Reader.Dispose();if(temporary!=null&&File.Exists(temporary))File.Delete(temporary);}
}
