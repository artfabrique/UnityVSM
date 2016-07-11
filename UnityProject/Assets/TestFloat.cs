using UnityEngine;
using System.Collections;

public class TestFloat : MonoBehaviour
{
    [SerializeField]private float _test;
    public float Test { get { return _test; } set { _test = value; } }

    [SerializeField]
    private bool _testbool;
    public bool Testbool { get { return _testbool; } set { _testbool = value; } }

    [SerializeField]
    private double _testdouble;
    public double Testdouble { get { return _testdouble; } set { _testdouble = value; } }

    [SerializeField]
    private int _testint;
    public int Testint { get { return _testint; } set { _testint = value; } }

    [SerializeField]
    private uint _testuint;
    public uint Testuint { get { return _testuint; } set { _testuint = value; } }

    [SerializeField]
    private long _testlong;
    public long Testlong { get { return _testlong; } set { _testlong = value; } }

    [SerializeField]
    private ulong _testulong;
    public ulong Testulong { get { return _testulong; } set { _testulong = value; } }
}
