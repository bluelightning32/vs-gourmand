using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Gourmand;

public readonly struct Interval<K, V> {
  public readonly K Begin;
  public readonly K End;
  public readonly V Value;

  public Interval(K begin, K end, V value) {
    Begin = begin;
    End = end;
    Value = value;
  }

  public override bool Equals([NotNullWhen(true)] object obj) {
    if (obj is not Interval<K, V> other) {
      return false;
    }
    return Begin.Equals(other.Begin) && End.Equals(other.End) &&
           Value.Equals(other.Value);
  }

  public override int GetHashCode() {
    return HashCode.Combine(Begin, End, Value);
  }

  public class CompareBegin : IComparer<Interval<K, V>> {
    public CompareBegin(IComparer<K> keyComparer) {
      _keyComparer = keyComparer;
    }

    public int Compare(Interval<K, V> x, Interval<K, V> y) {
      return _keyComparer.Compare(x.Begin, y.Begin);
    }

    private readonly IComparer<K> _keyComparer;
  }

  public static bool operator ==(Interval<K, V> left, Interval<K, V> right) {
    return left.Equals(right);
  }

  public static bool operator !=(Interval<K, V> left, Interval<K, V> right) {
    return !(left == right);
  }
}

public class ExclusiveIntervalDictionary<K, V> : ICollection<Interval<K, V>>,
                                                 ICollection {
  public ExclusiveIntervalDictionary(IComparer<K> comparer = null) {
    comparer ??= Comparer<K>.Default;
    _comparer = comparer;
    _set = new(new Interval<K, V>.CompareBegin(comparer));
  }

  public IEnumerable<Interval<K, V>> GetIntersecting(K begin, K end) {
    Interval<K, V> iBegin = new(begin, begin, default);
    Interval<K, V> iEnd = new(end, end, default);
    using IEnumerator<Interval<K, V>> range =
        _set.GetViewBetween(iBegin, iEnd).GetEnumerator();
    bool rangeEmpty = !range.MoveNext();
    if (rangeEmpty || _comparer.Compare(range.Current.Begin, begin) > 0) {
      // Either no intervals started within the range, or the first one to start
      // within the range did not start at the beginning. Check to see if an
      // interval started before the range and extends into the range.
      if (_set.Count > 0 && _comparer.Compare(_set.Min.Begin, begin) < 0) {
        var priorLast = _set.GetViewBetween(_set.Min, iBegin).Max;
        if (_comparer.Compare(priorLast.End, begin) > 0) {
          yield return priorLast;
        }
      }
    }
    if (rangeEmpty) {
      yield break;
    }
    Interval<K, V> previous = range.Current;
    while (range.MoveNext()) {
      yield return previous;
      previous = range.Current;
    }
    if (_comparer.Compare(previous.Begin, end) < 0) {
      yield return previous;
    }
  }

  public bool RemoveIntersecting(K begin, K end) {
    Interval<K, V> iBegin = new(begin, begin, default);
    Interval<K, V> iEnd = new(end, end, default);
    var intersectingStarts = _set.GetViewBetween(iBegin, iEnd).ToList();
    bool foundAny = false;
    if (intersectingStarts.Count > 0) {
      foreach (Interval<K, V> interval in intersectingStarts) {
        _set.Remove(interval);
        foundAny = true;
      }
      Interval<K, V> last = intersectingStarts[^1];
      if (_comparer.Compare(last.End, end) > 0) {
        _set.Add(new(end, last.End, last.Value));
      }
    }
    if (intersectingStarts.Count == 0 ||
        _comparer.Compare(intersectingStarts[0].Begin, begin) > 0) {
      if (_set.Count > 0 && _comparer.Compare(_set.Min.Begin, begin) < 0) {
        var priorLast = _set.GetViewBetween(_set.Min, iBegin).Max;
        if (_comparer.Compare(priorLast.End, begin) > 0) {
          foundAny = true;
          _set.Remove(priorLast);
          _set.Add(new(priorLast.Begin, begin, priorLast.Value));
        }
      }
    }
    return foundAny;
  }

  /// <summary>
  /// Add a new interval to the dictionary. The new interval is not merged with
  /// any adjacent intervals that have the same value.
  /// </summary>
  /// <param name="interval">
  /// The interval to add
  /// </param>
  /// <exception cref="ArgumentException">
  /// The new interval intersects with existing intervals
  /// </exception>
  public void Add(Interval<K, V> interval) {
    Interval<K, V> iBegin = new(interval.Begin, interval.Begin, default);
    Interval<K, V> iEnd = new(interval.End, interval.End, default);
    var beforeBegin = _set.GetViewBetween(_set.Min, iBegin);
    if (beforeBegin.Count > 0) {
      if (_comparer.Compare(beforeBegin.Max.End, interval.Begin) > 0) {
        throw new ArgumentException(
            "The new interval intersects with existing intervals.");
      }
    }
    var afterBegin = _set.GetViewBetween(iBegin, iEnd);
    if (afterBegin.Count > 0) {
      if (_comparer.Compare(afterBegin.Min.Begin, interval.End) < 0) {
        throw new ArgumentException(
            "The new interval intersects with existing intervals.");
      }
    }
    _set.Add(interval);
  }

  public void Add(K begin, K end, V value) {
    Add(new Interval<K, V>(begin, end, value));
  }

  public void Clear() { _set.Clear(); }

  public bool Contains(Interval<K, V> item) { return _set.Contains(item); }

  public void CopyTo(Interval<K, V>[] array, int arrayIndex) {
    _set.CopyTo(array, arrayIndex);
  }

  public bool Remove(Interval<K, V> item) { return _set.Remove(item); }

  public IEnumerator<Interval<K, V>> GetEnumerator() {
    return _set.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

  public void CopyTo(Array array, int index) {
    ((ICollection)_set).CopyTo(array, index);
  }

  private readonly SortedSet<Interval<K, V>> _set;
  private readonly IComparer<K> _comparer;

  public int Count => _set.Count;

  public bool IsReadOnly => false;

  public bool IsSynchronized => throw new NotImplementedException();

  public object SyncRoot => throw new NotImplementedException();
}
