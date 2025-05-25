using System;
using Unity.Collections;

public static class MathUtility
{
    public static int MinIndex<TDataType>(this NativeArray<TDataType> array)
        where TDataType : struct, IComparable<TDataType>
    {
        return MaxIndex(array, -1);
    }

    public static int MaxIndex<TDataType>(this NativeArray<TDataType> array)
        where TDataType : struct, IComparable<TDataType>
    {
        return MaxIndex(array, 1);
    }

    static int MaxIndex<TDataType>(NativeArray<TDataType> array, int coefficient)
        where TDataType : struct, IComparable<TDataType>
    {
        int index = 0;
        TDataType minValue = array[0];

        for (int i = 1; i < array.Length; i++)
        {
            TDataType value = array[i];
            if (value.CompareTo(minValue) * coefficient > 0)
            {
                index = i;
                minValue = value;
            }
        }

        return index;
    }
}
