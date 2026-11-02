using System.IO;
using System.Numerics;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CrateScope;
public class Track {
 public string Path {get;set;}=""; public string Name {get;set;}="";
 public string Key {get;set;}="Offen"; public string Camelot {get;set;}="—";
 public double Bpm {get;set;} public double Energy {get;set;} public double Duration {get;set;}
 public string Evidence {get;set;}=""; public string Alternatives {get;set;}="";
 public double KeySeparation {get;set;} public double TempoRegularity {get;set;}
 public double[] Wave {get;set;}=[]; public double[] Cues {get;set;}=[];
 public double Seconds {get;set;} public bool Manual {get;set;}
 public string OriginalKey {get;set;}=""; public double OriginalBpm {get;set;}
 public double RmsDb {get;set;}
}
public static class Engine {
 public static readonly string[] Notes=["C","Db","D","Eb","E","F","Gb","G","Ab","A","Bb","B"];
 public static readonly string[] Keys=Enumerable.Range(0,24).Select(i=>Notes[i%12]+(i<12?" major":" minor")).ToArray();
 static readonly int[] Maj=[8,3,10,5,12,7,2,9,4,11,6,1], Min=[5,12,7,2,9,4,11,6,1,8,3,10];
 static readonly double[][] Profiles=[[6.35,2.23,3.48,2.33,4.38,4.09,2.52,5.19,2.39,3.66,2.29,2.88],[6.33,2.68,3.52,5.38,2.60,3.53,2.54,4.75,3.98,2.69,3.34,3.17]];
 public static string Camelot(string key) {int i=Array.IndexOf(Keys,key);return i<0?"—":(i<12?Maj[i%12]:Min[i%12])+(i<12?"B":"A");}
 static double Clamp(double x)=>Math.Clamp(x,0,1);
 static double Median(IEnumerable<double> values) {var a=values.Order().ToArray();return a.Length==0?0:a[a.Length/2];}
 static void Fft(Complex[] x) {
  int n=x.Length; for(int i=1,j=0;i<n;i++){int b=n>>1;for(;(j&b)!=0;b>>=1)j^=b;j^=b;if(i<j)(x[i],x[j])=(x[j],x[i]);}
  for(int len=2;len<=n;len<<=1){var wl=Complex.FromPolarCoordinates(1,-2*Math.PI/len);for(int i=0;i<n;i+=len){Complex w=1;for(int j=0;j<len/2;j++){var u=x[i+j];var v=x[i+j+len/2]*w;x[i+j]=u+v;x[i+j+len/2]=u-v;w*=wl;}}}
 }
 static double Corr(double[] a,double[] b){double am=a.Average(),bm=b.Average(),ab=0,aa=0,bb=0;for(int i=0;i<a.Length;i++){double x=a[i]-am,y=b[i]-bm;ab+=x*y;aa+=x*x;bb+=y*y;}return ab/Math.Sqrt(Math.Max(1e-20,aa*bb));}
 public static Track Analyze(string path,Action<double,string>? progress=null) {
  var sw=Stopwatch.StartNew();progress?.Invoke(0,"Audio lesen");
  using var reader=new AudioFileReader(path);
  if(reader.TotalTime.TotalSeconds<2||reader.TotalTime.TotalSeconds>1800)throw new InvalidDataException("Track muss zwischen 2 Sekunden und 30 Minuten lang sein.");
  // Select strongest channel in each read block to preserve anti-phase stereo.
  var mono=new StrongestChannel(reader);var resampled=new WdlResamplingSampleProvider(mono,22050);
  List<float> samples=new();float[] read=new float[22050];int count;
  while((count=resampled.Read(read,0,read.Length))>0){for(int i=0;i<count;i++){if(!float.IsFinite(read[i]))throw new InvalidDataException("Ungültige Samples.");samples.Add(read[i]);} if(samples.Count>22050*1800)throw new InvalidDataException("Track zu lang.");}
  var y=samples.ToArray();double peak=y.Max(v=>Math.Abs(v));if(peak<1e-6)throw new InvalidDataException("Nur Stille: keine Tonart oder BPM messbar.");
  double duration=y.Length/22050.0, rms=Math.Sqrt(y.Sum(v=>(double)v*v)/y.Length);
  const int n=4096,hop=220;int frames=Math.Max(1,(y.Length-n)/hop+1);
  var chroma=new double[12];var envelope=new double[frames];var level=new double[frames];var local=new List<double[]>();var prev=new double[n/2];double brightness=0;
  for(int f=0;f<frames;f++){
   if(f%100==0)progress?.Invoke(.1+.65*f/frames,"Spektrum und Rhythmus");
   var x=new Complex[n];double sum=0;for(int i=0;i<n;i++){double v=y[f*hop+i]/peak;sum+=v*v;x[i]=v*(.5-.5*Math.Cos(2*Math.PI*i/(n-1)));}level[f]=Math.Sqrt(sum/n);Fft(x);
   var mag=new double[n/2];double flux=0,mass=0,weighted=0;for(int k=1;k<mag.Length;k++){mag[k]=x[k].Magnitude;flux+=Math.Max(0,Math.Log(1+mag[k])-Math.Log(1+prev[k]));mass+=mag[k];weighted+=mag[k]*k*22050/n;}envelope[f]=flux;prev=mag;brightness+=weighted/Math.Max(mass,1e-10);
   var c=new double[12];for(int k=12;k<558;k++){if(mag[k]<=mag[k-1]||mag[k]<=mag[k+1]||mag[k]<.02)continue;double l=Math.Log(mag[k-1]+1e-12),m=Math.Log(mag[k]+1e-12),r=Math.Log(mag[k+1]+1e-12);double delta=.5*(l-r)/(l-2*m+r);double freq=(k+Math.Clamp(delta,-.5,.5))*22050/n;double midi=69+12*Math.Log2(freq/440);int note=((int)Math.Round(midi)%12+12)%12;c[note]+=Math.Sqrt(mag[k]);}
   if(level[f]>.005)for(int k=0;k<12;k++)chroma[k]+=c[k];
   if(f%200==0)local.Add(c);
  }
  var scores=Enumerable.Range(0,24).Select(i=>new{Index=i,Value=Corr(chroma,Enumerable.Range(0,12).Select(k=>Profiles[i/12][(k-i%12+12)%12]).ToArray())}).OrderByDescending(x=>x.Value).ToArray();
  string key=scores[0].Value>.45?Keys[scores[0].Index]:"Offen";double separation=scores[0].Value-scores[1].Value;
  // Onset peaks and interval consensus, then periodicity fallback.
  double threshold=envelope.Max()*.15;var peaks=new List<int>();for(int i=2;i<frames-2;i++)if(envelope[i]>threshold&&envelope[i]>=envelope[i-1]&&envelope[i]>envelope[i+1]){if(peaks.Count==0||i-peaks[^1]>18)peaks.Add(i);else if(envelope[i]>envelope[peaks[^1]])peaks[^1]=i;}
  double fps=22050.0/hop;var intervals=peaks.Zip(peaks.Skip(1),(a,b)=>(b-a)/fps).ToArray();double period=Median(intervals),regularity=intervals.Length>3?intervals.Count(v=>Math.Abs(v-period)<period*.12)/(double)intervals.Length:0;double bpm=0;
  if(regularity>.7){var regular=intervals.Where(v=>Math.Abs(v-period)<period*.12).ToArray();bpm=60/regular.Average();}
  else if(peaks.Count>4){double best=0;int bestLag=0;for(int lag=(int)(fps*.3);lag<Math.Min(frames/2,fps*1.2);lag++){double s=0,a=0,b=0;for(int i=lag;i<frames;i++){s+=envelope[i]*envelope[i-lag];a+=envelope[i]*envelope[i];b+=envelope[i-lag]*envelope[i-lag];}double score=s/Math.Sqrt(Math.Max(a*b,1e-10));if(score>best){best=score;bestLag=lag;}}if(best>.2)bpm=60*fps/bestLag;regularity=best;}
  if(bpm<40||bpm>240)bpm=0;
  double db=20*Math.Log10(Math.Max(rms,1e-10));double energy=1+9*(.35*Clamp((db+45)/36)+.3*Clamp(peaks.Count/duration/4)+.2*Clamp(brightness/frames/5000)+.15*Clamp((bpm-60)/120));
  var cues=new List<double>{Array.FindIndex(level,v=>v>level.Max()*.01)*hop/22050.0};
  var changes=Enumerable.Range(1,local.Count-1).Select(i=>new{Time=i*200/fps,Value=1-Corr(local[i],local[i-1])}).OrderByDescending(v=>v.Value);
  foreach(var c in changes){if(c.Value>.2&&c.Time<duration-2&&cues.All(t=>Math.Abs(t-c.Time)>8))cues.Add(c.Time);if(cues.Count==8)break;}
  var wave=new double[1000];for(int i=0;i<y.Length;i++){int bin=Math.Min(999,(int)((long)i*1000/y.Length));wave[bin]=Math.Max(wave[bin],Math.Abs(y[i])/peak);}
  progress?.Invoke(1,"Fertig");return new Track{Path=System.IO.Path.GetFullPath(path),Name=System.IO.Path.GetFileNameWithoutExtension(path),Key=key,OriginalKey=key,Camelot=Camelot(key),Bpm=Math.Round(bpm,2),OriginalBpm=Math.Round(bpm,2),Energy=Math.Round(energy,1),Duration=duration,Wave=wave,Cues=cues.Order().ToArray(),Seconds=sw.Elapsed.TotalSeconds,RmsDb=db,KeySeparation=separation,TempoRegularity=regularity,Evidence=(key=="Offen"||separation<.08?"Tonart prüfen":"Tonales Profil klar")+(regularity<.7?" · Tempo prüfen":" · Puls regelmäßig"),Alternatives=string.Join(" / ",scores.Take(3).Select(s=>Keys[s.Index]))+ $" · Tempoalternativen: {bpm/2:F1} / {bpm*2:F1} BPM"};
 }
 class StrongestChannel(ISampleProvider source):ISampleProvider {
  public WaveFormat WaveFormat=>WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate,1);
  public int Read(float[] buffer,int offset,int count){int channels=source.WaveFormat.Channels;var temp=new float[count*channels];int got=source.Read(temp,0,temp.Length);var power=new double[channels];for(int i=0;i<got;i++)power[i%channels]+=temp[i]*temp[i];int selected=Array.IndexOf(power,power.Max());int frames=got/channels;for(int i=0;i<frames;i++)buffer[offset+i]=temp[i*channels+selected];return frames;}
 }
}
