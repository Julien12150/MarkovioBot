using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkovioBot.Markov {
	/// <summary>
	/// Represents a state in a Markov chain.
	/// </summary>
	/// <typeparam name="T">The type of the constituent parts of each state in the Markov chain.</typeparam>
	public class ChainState<T> : IEquatable<ChainState<T>> {
		private readonly T[] items;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChainState&lt;T&gt;"/> class with the specified items.
		/// </summary>
		/// <param name="items">An <see cref="IEnumerable&lt;T&gt;"/> of items to be copied as a single state.</param>
		public ChainState(IEnumerable<T> items) {
			if (items == null) {
				throw new ArgumentNullException("items");
			}

			this.items = items.ToArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChainState&lt;T&gt;"/> class with the specified items.
		/// </summary>
		/// <param name="items">A <see cref="T:T[]"/> of items to be copied as a single state.</param>
		public ChainState(T[] items) {
			if (items == null) {
				throw new ArgumentNullException("items");
			}

			this.items = new T[items.Length];
			Array.Copy(items, this.items, items.Length);
		}

		/// <summary>
		/// Determines whether two specified instances of <see cref="ChainState&lt;T&gt;"/> are equal.
		/// </summary>
		/// <param name="a">A <see cref="ChainState&lt;T&gt;"/>.</param>
		/// <param name="b">A <see cref="ChainState&lt;T&gt;"/>.</param>
		/// <returns>true if <paramref name="a"/> and <paramref name="b"/> represent the same state; otherwise, false.</returns>
		public static bool operator ==(ChainState<T> a, ChainState<T> b) {
			if (object.ReferenceEquals(a, b)) {
				return true;
			}

			if (((object)a == null) || ((object)b == null)) {
				return false;
			}

			return a.Equals(b);
		}

		/// <summary>
		/// Determines whether two specified instances of <see cref="ChainState&lt;T&gt;"/> are not equal.
		/// </summary>
		/// <param name="a">A <see cref="ChainState&lt;T&gt;"/>.</param>
		/// <param name="b">A <see cref="ChainState&lt;T&gt;"/>.</param>
		/// <returns>true if <paramref name="a"/> and <paramref name="b"/> do not represent the same state; otherwise, false.</returns>
		public static bool operator !=(ChainState<T> a, ChainState<T> b) {
			return !(a == b);
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override int GetHashCode() {
			var code = this.items.Length.GetHashCode();

			for (int i = 0; i < this.items.Length; i++) {
				code ^= this.items[i].GetHashCode();
			}

			return code;
		}

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="object"/> to compare with the current <see cref="object"/>.</param>
		/// <returns>true if the specified <see cref="object"/> is equal to the current <see cref="object"/>; otherwise, false.</returns>
		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}

			return this.Equals(obj as ChainState<T>);
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public bool Equals(ChainState<T> other) {
			if ((object)other == null) {
				return false;
			}

			if (this.items.Length != other.items.Length) {
				return false;
			}

			for (int i = 0; i < this.items.Length; i++) {
				if (!this.items[i].Equals(other.items[i])) {
					return false;
				}
			}

			return true;
		}
	}
}
