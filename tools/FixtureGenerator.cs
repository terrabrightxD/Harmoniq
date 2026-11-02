using System;
using System.IO;
using System.Text;
// Original synthetic fixtures. No third-party music or Python required.
public static class FixtureGenerator {
 public static void Write(string path,int root,bool minor,double bpm,int seconds,double gain,int silence,bool arrangement){
  const int sr=22050;var y=new double[seconds*sr];int[] chords={0,5,7,0};
  for(int beat=0;beat*60.0/bpm<seconds;beat++){
   double start=beat*60.0/bpm;int offset=(int)(start*sr),length=Math.Min((int)(.85*60/bpm*sr),y.Length-offset);
   int chordRoot=root+(arrangement?chords[(beat/4)%4]:0);
   int[] intervals={0,minor?3:4,7,12};double[] weights={1,.65,.55,.35};
   for(int j=0;j<4;j++){double freq=440*Math.Pow(2,(48+chordRoot+intervals[j]-69)/12.0);for(int i=0;i<length;i++){double t=i/(double)sr,env=Math.Min(t/.015,1)*Math.Exp(-t*3);y[offset+i]+=weights[j]*.17*env*(Math.Sin(2*Math.PI*freq*t)+.15*Math.Sin(4*Math.PI*freq*t));}}
   int n=Math.Min((int)(.09*sr),y.Length-offset);for(int i=0;i<n;i++){double t=i/(double)sr;y[offset+i]+=.25*Math.Sin(2*Math.PI*70*t)*Math.Exp(-t*55);}
   if(arrangement&&start>8&&start<16)for(int i=0;i<length;i++)y[offset+i]*=.35;
  }
  int count=y.Length+silence*sr;using(var f=new BinaryWriter(File.Create(path))){f.Write(Encoding.ASCII.GetBytes("RIFF"));f.Write(36+count*2);f.Write(Encoding.ASCII.GetBytes("WAVEfmt "));f.Write(16);f.Write((short)1);f.Write((short)1);f.Write(sr);f.Write(sr*2);f.Write((short)2);f.Write((short)16);f.Write(Encoding.ASCII.GetBytes("data"));f.Write(count*2);for(int i=0;i<silence*sr;i++)f.Write((short)0);foreach(var sample in y)f.Write((short)Math.Max(-32768,Math.Min(32767,Math.Floor(sample*gain*32768))));}
 }
}
