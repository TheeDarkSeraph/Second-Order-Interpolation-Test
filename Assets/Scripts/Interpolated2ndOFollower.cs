using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class Interpolated2ndOFollower{
    // TODO: Add handling of rotation in case the Vector3 was a rotation vector
    //      and we want to adjust going beyond 4*pi or such... next update
    [Range(0, 100)] public float f;
    [Range(0, 5)] public float z = 0.5f;
    [Range(-20, 20)] public float r = 0;
    public Transform follower, target;
    public bool ignoreX = false, ignoreY = false, ignoreZ = false;
    private float k1, k2, k3;
    private Vector3 xPrev, y, yDer;
    private float TCrit;
    private float useT;
    public void Init() {
        xPrev = y = target.position;
        yDer = Vector3.zero;
        if (f == 0) f = 0.0001f;
    }
    public void Follow(float timePass) {
        useT = timePass;
        CalcKs();
        InterpolatePosition(target.position);
    }
    private void CalcKs() {
        float temp = Mathf.PI * f;
        k1 = z / (temp);
        temp *= 2;
        k2 = 1 / (temp * temp);
        k3 = r * z / temp;
        TCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
        // 0.8f to be safe, but T needs to be less than this to have a decreasing magnitude
        //  so that the system stays stable
    }


    private void InterpolatePosition(Vector3 targetPos) {
        Vector3 newPosition = Vector3.zero;
        newPosition = Interpolate2ndOrder(targetPos);

        if (ignoreX)
            newPosition.x = follower.position.x;
        if (ignoreY)
            newPosition.y = follower.position.y;
        if (ignoreZ)
            newPosition.z = follower.position.z;

        follower.position = newPosition;
    }
    private Vector3 Interpolate2ndOrder(Vector3 xCur) { // T critical constraint
        float T = useT;
        //Update xDer and calculate x[n+1]

        Vector3 xDer = (xCur - xPrev) / T;
        xPrev = xCur;

        int numOfInterpols = Mathf.CeilToInt(Time.deltaTime / TCrit);
        T = T / numOfInterpols; // apply the movement in smaller steps to compensate

        for (int i = 0; i < numOfInterpols; i++) {
            y = y + T * yDer;
            Vector3 yDer2 = (xCur + k3 * xDer - y - k1 * yDer) / k2;
            yDer = yDer + T * yDer2;
        }
        return y;
    }




    /*
    private float _w, _z, _d;
    private void CalcZeroPoleVals() {
        float temp = Mathf.PI * f;
        k1 = z / (temp);
        temp *= 2;

        _w = temp;
        _z = z;
        _d = _w * Mathf.Sqrt(Mathf.Abs(z * z - 1));

        k2 = 1 / (_w * _w);
        k3 = r * z / _w;
    }

    private Vector3 Interpolate2ndOrder_ConstrainedK2Variant(Vector3 xCur) {
        float T = useT;
        //Update xDer and calculate x[n+1]

        Vector3 xDer = (xCur - xPrev) / T;
        xPrev = xCur;

        //clamp k2 to guarantee stability // variant 1
        //float k2_stable = Mathf.Max(k2, 1.1f * (T * T / 4 + T * k1 / 2));

        // ensures NO jitters due to negative eigen values // varient 2
        // This or above
        float k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);
        // this is NOT physically correct, it just stops the system from failing or becoming
        //      very unstable in case of a lag or something causing 'T' to be too big
        y = y + T * yDer;
        Vector3 yDer2 = (xCur + k3 * xDer - y - k1 * yDer) / k2_stable;
        yDer = yDer + T * yDer2;
        return y;
    }

    // final variant is pole zero matching to get best accuracy
    private Vector3 Interpolate2ndOrder_ZeroPole(Vector3 xCur) {
        float T = useT;
        //Update xDer and calculate x[n+1]

        float k1_stable, k2_stable;

        Vector3 xDer = (xCur - xPrev) / T;
        xPrev = xCur;

        if (_w * T < _z) {
            k1_stable = k1;
            k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);
        } else {
            float t1 = Mathf.Exp(-_z * _w * T);
            float alpha = 2 * t1 * (_z <= 1 ? Mathf.Cos(T * _d) : (float)System.Math.Cosh(T * _d));
            float beta = t1 * t1;
            float t2 = T / (1 + beta - alpha);
            k1_stable = (1 - beta) * t2;
            k2_stable = T * t2;
        }

        // this is NOT physically correct, it just stops the system from failing or becoming
        //      very unstable in case of a lag or something causing 'T' to be too big
        y = y + T * yDer;
        Vector3 yDer2 = (xCur + k3 * xDer - y - k1_stable * yDer) / k2_stable;
        yDer = yDer + T * yDer2;
        return y;
    }
    //*/
}
