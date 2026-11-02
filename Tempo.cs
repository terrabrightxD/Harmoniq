namespace CrateScope;
public static class Tempo {
 // Prefer the shortest strongly supported period, reducing octave-down errors.
 // This is a metric-level preference, not evidence for a particular time signature.
 public static (double bpm,double strength) Periodicity(double[] onset,double fps) {
  int max=Math.Min(onset.Length/2,(int)(fps*1.2));var scores=new double[max+2];
  int min=(int)(fps*.3);
  for(int lag=min-1;lag<=max;lag++){double sum=0,a=0,b=0;for(int i=lag;i<onset.Length;i++){sum+=onset[i]*onset[i-lag];a+=onset[i]*onset[i];b+=onset[i-lag]*onset[i-lag];}scores[lag]=sum/Math.Sqrt(Math.Max(a*b,1e-10));}
  double best=scores.Max();if(best<.2)return(0,best);
  int chosen=Enumerable.Range(min,max-min).Where(i=>scores[i]>=best*.92&&scores[i]>=scores[i-1]&&scores[i]>=scores[i+1]).DefaultIfEmpty(Array.IndexOf(scores,best)).First();
  double l=scores[chosen-1],m=scores[chosen],r=scores[chosen+1];double denominator=l-2*m+r;
  double delta=Math.Abs(denominator)<1e-10?0:Math.Clamp(.5*(l-r)/denominator,-.5,.5);
  return(60*fps/(chosen+delta),scores[chosen]);
 }
}
