using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//一个自制的提供高效内存管理的动态数组
namespace MonoCirclesMove
{
    //值类型
    public unsafe class DynamicArraySrtuct<T> where T : unmanaged
    {
        private T* _buffer;
        private int _count;
        private int _capacity;

        public DynamicArraySrtuct(int initialCapacity = 4)
        {
            _buffer = (T*)Marshal.AllocHGlobal(sizeof(T) * initialCapacity);
            _count = 0;
            _capacity = initialCapacity;
        }

        public void Add(T item)
        {
            if (_count == _capacity)
            {
                _capacity *= 2;
                T* newBuffer = (T*)Marshal.AllocHGlobal(sizeof(T) * _capacity);
                Buffer.MemoryCopy(_buffer, newBuffer, _capacity * sizeof(T), _count * sizeof(T));
                Marshal.FreeHGlobal((IntPtr)_buffer);
                _buffer = newBuffer;
            }

            _buffer[_count] = item;
            ++_count;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                return _buffer[index];
            }

            set
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                _buffer[index] = value;
            }
        }

        public int Count => _count;


        ~DynamicArraySrtuct()
        {
            Marshal.FreeHGlobal((IntPtr)_buffer);
        }
    }
    public class DynamicArray<T>: IEnumerable<T>
    {
        private T[] _buffer;
        private int _count;
        private int _capacity;

        public DynamicArray(int initialCapacity = 4)
        {
            _buffer = new T[initialCapacity];
            _count = 0;
            _capacity = initialCapacity;
        }

        public void Add(T item)
        {
            if (_count == _capacity)
            {
                _capacity *= 2;
                T[] newBuffer = new T[_capacity];
                Array.Copy(_buffer, newBuffer, _count);
                _buffer = newBuffer;
            }

            _buffer[_count] = item;
            ++_count;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException();

            Array.Copy(_buffer, index + 1, _buffer, index, _count - index - 1);

            --_count;

            // Shrinks the array if needed
            if (_count <= _capacity / 4)
            {
                _capacity /= 2;
                T[] newBuffer = new T[_capacity];
                Array.Copy(_buffer, newBuffer, _count);
                _buffer = newBuffer;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                return _buffer[index];
            }

            set
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                _buffer[index] = value;
            }
        }

        public int Count => _count;

        // Implementation for the GetEnumerator method.
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _buffer[i];
            }
        }

        // Explicit interface implementation for the non-generic IEnumerator
        // Required as IEnumerable<T> inherits from IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
