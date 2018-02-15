using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Filters {

    class CircularBufferEnum<T> : IEnumerator<T>, IEnumerator {
        int pos_ = -1;
        CircularBuffer<T> buffer_;

        public CircularBufferEnum(CircularBuffer<T> buffer) {
            buffer_ = buffer;
        }

        public T Current {
            get { return buffer_[pos_]; }
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        object IEnumerator.Current {
            get { return buffer_[pos_]; }
        }

        public bool MoveNext() {
            pos_++;
            return pos_ < buffer_.Count;
        }

        public void Reset() {
            pos_ = -1;
        }

    } 

    /// <summary>
    /// Implement a cicular buffer with the last inserted element at index 0.
    /// </summary>
    /// <typeparam name="T">Type of element</typeparam>
    class CircularBuffer<T> : IList<T>, ICollection<T>, IEnumerable<T>, ICollection, IEnumerable {
        /// <summary>
        /// Buffer inner data
        /// </summary>
        protected T[] data_ = null;

        /// <summary>
        /// 
        /// </summary>
        private int count_ = 0;

        /// <summary>
        /// Copy of an empty element
        /// </summary>
        private T empty_;

        /// <summary>
        /// Current base index
        /// </summary>
        protected int base_ = 0;

        /// <summary>
        /// Create an instance of <typeparamref name="CircularBuffer"/> with a size of <paramref name="count"/> elements.
        /// </summary>
        /// <param name="count">Count of element for the new <typeparamref name="CircularBuffer"/></param>
        /// <param name="empty">A copy of an "empty" element</param>
        public CircularBuffer(int count, T empty) {

            count_ = count;
            empty_ = empty;
            data_ = new T[count];

            Clear();

        }

        /// <summary>
        /// Add an item at position 0.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public virtual void Add(T item) {

            base_++;
            if (base_ >= count_) base_ = 0;

            data_[base_] = item;

        }

        /// <summary>
        /// Clear the <typeparamref name="CircularBuffer"/> by affecting all element with a an "empty" element.
        /// </summary>
        public void Clear() {

            for (int i = 0; i < count_; i++)
                data_[i] = empty_;

        }

        /// <summary>
        /// Test if an item is in the <typeparamref name="CircularBuffer"/>
        /// </summary>
        /// <param name="item">Item to test to</param>
        /// <returns>True if <paramref name="item"/> is in the <typeparamref name="CircularBuffer"/>, False otherwise.</returns>
        public bool Contains(T item) {

            return IndexOf(item) != -1;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex) {

            for (int i = 0; i < count_; i++)
                array[arrayIndex + i] = this[i];

        }

        public int Count {

            get {

                return data_.Length;

            }

        }

        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Remove the <paramref name="item"/> element in the <typeparamref name="CircularBuffer"/>.
        /// </summary>
        /// <param name="item">Element t remove.</param>
        /// <returns>True if the element was found and removed, False otherwise.</returns>
        public bool Remove(T item) {

            int i = IndexOf(item);

            if (i != -1) {

                RemoveAt(i);
                return true;

            } else
                return false;
        }

        public IEnumerator<T> GetEnumerator() {
            return new CircularBufferEnum<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new CircularBufferEnum<T>(this);
        }

        public void CopyTo(Array array, int index) {
            for (int i = 0; i < count_; i++)
                array.SetValue(this[i], index + i);
        }

        public bool IsSynchronized {
            get { return false; }
        }

        public object SyncRoot {
            get { return null; }
        }

        /// <summary>
        /// Return the index of <paramref name="item"/> in the <typeparamref name="CircularBuffer"/>.
        /// </summary>
        /// <param name="item">Item to search to.</param>
        /// <returns>Index of then<paramref name="item"/> element.</returns>
        public int IndexOf(T item) {

            for (int i = 0; i < count_; i++)
                if (item.Equals(this[i])) return i;

            return -1;

        }

        /// <summary>
        /// Insert the <paramref name="item"/> element at <paramref name="index"/> position.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item) {

            for (int i = count_ - 2; i >= index; i--)
                this[i + 1] = this[i];

            this[index] = item;

        }

        /// <summary>
        /// Remove element at position <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Index of the element to remove.</param>
        public void RemoveAt(int index) {

            for (int i = index; i < count_ - 1; i++)
                this[i] = this[i + 1];

            this[count_ - 1] = empty_;

        }

        private int GetIndex(int index) {
            if ((index < 0) || (index > count_))
                throw new IndexOutOfRangeException();

            return (count_ + base_ - index) % count_;
        }
        /// <summary>
        /// Return element at position <paramref name="index"/>. Element with index 0 is the last added element.
        /// </summary>
        /// <param name="index">Index of the element in the <typeparamref name="CircularBuffer"/>.</param>
        /// <returns>Element at position <paramref name="index"/>.</returns>
        public virtual T this[int index] {
            get {
                return data_[GetIndex(index)];
            }
            set {
                data_[GetIndex(index)] = value;
            }
        }
    }
}
