using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Farseer.Util;

public readonly record struct Coord2D(int X, int Z)
{
  public float Len()
  {
    return GameMath.Sqrt(X * X + Z * Z);
  }
}

public sealed class SpiralWalker(Coord2D center, int radius) : IEnumerable<Coord2D>, IEnumerator<Coord2D>
{
  private readonly int _radius = radius;
  private readonly Coord2D _center = center;

  private int _x = center.X;
  private int _z = center.Z;

  private int _dx = 0;
  private int _dz = -1;

  private Coord2D _currentCoordinate;

  public Coord2D Current => _currentCoordinate;

  object IEnumerator.Current => _currentCoordinate;

  public void Dispose() { }

  public IEnumerator<Coord2D> GetEnumerator()
  {
    return this;
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return this;
  }

  public bool MoveNext()
  {
    var done = _x < -_radius || _x > _radius || _z < -_radius || _x > _radius;

    _currentCoordinate = new Coord2D(_x, _z);

    if (_x == _z || (_x < 0 && _x == -_z) || (_x > 0 && _x == 1 - _z))
    {
      var t = _dx;
      _dx = -_dz;
      _dz = t;
    }

    _x += _dx;
    _z += _dz;

    return !done;
  }

  public void Reset()
  {
    _x = _center.X;
    _z = _center.Z;
    _dx = 0;
    _dz = -1;
  }
}
