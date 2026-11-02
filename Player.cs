using NAudio.Wave;
namespace CrateScope;
public sealed class Player : IDisposable {
 readonly AudioSource source;
 readonly WaveOutEvent output;
 double seekAnchor;long byteAnchor;
 public Player(AudioSource audio){source=audio;output=new WaveOutEvent{DesiredLatency=100};try{output.Init(source.Reader);}catch{output.Dispose();source.Dispose();throw;}}
 public double Duration=>source.Reader.TotalTime.TotalSeconds;
 // Device cursor, not decoder read-ahead: cue placement follows audible playback.
 public double Position {get=>Math.Clamp(seekAnchor+Math.Max(0,output.GetPosition()-byteAnchor)/(double)source.Reader.WaveFormat.AverageBytesPerSecond,0,Duration);set{bool resume=Playing;output.Stop();seekAnchor=Math.Clamp(value,0,Duration);source.Reader.CurrentTime=TimeSpan.FromSeconds(seekAnchor);byteAnchor=output.GetPosition();if(resume)output.Play();}}
 public bool Playing=>output.PlaybackState==PlaybackState.Playing;
 public float Volume{set=>output.Volume=Math.Clamp(value,0,1);}
 public long OutputBytes=>output.GetPosition();
 public void Play(){if(Position>=Duration-.05)Position=0;output.Play();}
 public void Pause()=>output.Pause();
 public void Dispose(){output.Stop();output.Dispose();source.Dispose();}
}
