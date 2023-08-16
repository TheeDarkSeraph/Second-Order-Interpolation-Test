using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SecondOrderFollower))]
[RequireComponent(typeof(AnimationCurve))]
//[ExecuteAlways]
public class SecondOrderCurveDrawer : MonoBehaviour{
    private SecondOrderFollower sof;
    public bool modelCurve = false;
    public AnimationCurve responseCurve;
    //[Range(1,10000)]public float clampMax;
    [Range(0.001f,10)]public float Tau=1f/60f;
    public float curveTimeLength = 5f;
    // This will only work with the original slow down T one, the original varient
    private float f, z, r,y,yDer;
    private readonly float eps = 0.001f;
    private float TCrit,k1,k2,k3;
    // Start is called before the first frame update
    void Start(){
        sof = GetComponent<SecondOrderFollower>();
    }
    // Update is called once per frame
    void Update(){
        if (modelCurve) {
            if (ConstsChanged()) {
                f = sof.f;
                z = sof.z;
                r = sof.r;
                CalcKs();
                RedrawCurve();
            }
        }
    }

    private void CalcKs() {
        float temp = Mathf.PI * f;
        k1 = z / (temp);
        temp *= 2;
        k2 = 1 / (temp * temp);
        k3 = r * z / temp;
        TCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
    }
    private bool ConstsChanged() {
        return f != sof.f || z != sof.z || r != sof.r;
    }
    private void RedrawCurve() {

        if (responseCurve == null)
            return;
        y = 0;
        yDer = 0;
        List<Keyframe> frames = new List<Keyframe>() ;
        Keyframe kf = new Keyframe(0, 0);
        frames.Add(kf);
        float curT = 0;
        float useT = Mathf.Max(Mathf.Min(curveTimeLength - Tau, Tau), 0.001f);
        curT += useT;
        Interpolate2ndOrder(useT, 0, 1,curT, frames);
        while (curT < curveTimeLength - eps) {
            useT = Mathf.Max(Mathf.Min(curveTimeLength - Tau, Tau),0.001f);
            curT += useT;
            Interpolate2ndOrder(useT, 1, 1, curT, frames);
        }
        int count = 0;
        for (int i = 1; i < frames.Count; i++) {
            float difY = frames[i].value - frames[i-1].value;
            float difX = frames[i].time - frames[i-1].time;
            kf = frames[i];
            kf.inTangent = difY / difX;
            frames[i] = kf;
            kf = frames[i-1];
            kf.outTangent = difY / difX;
            frames[i-1] = kf;
            count++;
            if (count > 400)
                break;
        }
        kf = frames[frames.Count-1];
        kf.outTangent = 0;
        frames[frames.Count - 1] = kf;
        responseCurve.keys = frames.ToArray();
    }

    private void Interpolate2ndOrder(float T, float xPrev, float xCur,float curTime, List<Keyframe> frames) { // T critical constraint
        //Update xDer and calculate x[n+1]

        float xDer = (xCur - xPrev) / T;
        xPrev = xCur;

        int numOfInterpols = Mathf.CeilToInt(T / TCrit);
        T = T / numOfInterpols; // apply the movement in smaller steps to compensate

        for (int i = 0; i < numOfInterpols; i++) {
            y = y + T * yDer;
            float yDer2 = (xCur + k3 * xDer - y - k1 * yDer) / k2;
            yDer = yDer + T * yDer2;
            Keyframe kf = new Keyframe(curTime, y);
            frames.Add(kf);
            curTime += T;
        }
    }
}
