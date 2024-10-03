using System;
using System.Runtime.CompilerServices;

namespace QuickBin.ChainExtensions {
	public static class ChainExtensions {
		/// <summary>Executes an action.</summary>
		/// <param name="action">The action to execute.</param>
		/// <returns><c>@this</c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TChain Then<TChain>(this TChain @this, Action action) {
			action();
			return @this;
		}
		
		/// <summary>Assigns a value to the out parameter.</summary>
		/// <param name="value">The value to assign.</param>
		/// <param name="variable">The variable to assign to.</param>
		/// <returns><c>@this</c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TChain Assign<TChain, T>(this TChain @this, T value, out T variable) {
			variable = value;
			return @this;
		}
		
		/// <summary>Returns an unrelated object.</summary>
		/// <returns><c>value</c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Output<T>(this object _, T value) => value;
		
		/// <summary>Executes an action if a condition is met.</summary>
		/// <param name="condition">The condition to check.</param>
		/// <param name="action">The action to execute.</param>
		/// <returns><c>@this</c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TChain When<TChain>(this TChain @this, bool condition, Action<TChain> action) {
			if (condition) action(@this);
			return @this;
		}
	}
}