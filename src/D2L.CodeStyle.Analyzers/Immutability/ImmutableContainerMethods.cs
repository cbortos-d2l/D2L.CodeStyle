﻿using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	public static class ImmutableContainerMethods {
		/// <summary>
		/// A list of immutable container types (i.e., types that hold other types)
		/// </summary>
		private static readonly ImmutableDictionary<string, string[]> ImmutableContainerTypes = new Dictionary<string, string[]> {
			["D2L.LP.Utilities.DeferredInitializer"] = new[] { "Value" },
			["D2L.LP.Extensibility.Activation.Domain.IPlugins"] = new[] { "[]" },
			["System.Collections.Immutable.IImmutableSet"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableArray"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableDictionary"] = new[] { "[].Key", "[].Value" },
			["System.Collections.Immutable.ImmutableHashSet"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableList"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableQueue"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableSortedDictionary"] = new[] { "[].Key", "[].Value" },
			["System.Collections.Immutable.ImmutableSortedSet"] = new[] { "[]" },
			["System.Collections.Immutable.ImmutableStack"] = new[] { "[]" },
			["System.Collections.Generic.IReadOnlyCollection"] = new[] { "[]" },
			["System.Collections.Generic.IReadOnlyList"] = new[] { "[]" },
			["System.Collections.Generic.IReadOnlyDictionary"] = new[] { "[].Key", "[].Value" },
			["System.Collections.Generic.IEnumerable"] = new[] { "[]" },
			["System.Lazy"] = new[] { "Value" },
			["System.Nullable"] = new[] { "Value" },
			["System.Tuple"] = new[] { "Item1", "Item2", "Item3", "Item4", "Item5", "Item6" }
		}.ToImmutableDictionary();

		public static bool IsAnImmutableContainerType( this ITypeSymbol type ) {
			return ImmutableContainerTypes.ContainsKey( type.GetFullTypeName() );
		}

		public static string[] GetImmutableContainerTypePrefixes( this ITypeSymbol type ) {
			if (ImmutableContainerTypes.TryGetValue( type.GetFullTypeName(), out string[] result )) {
				return result;
			}

			// Not found, so return an empty array
			return new string[] { };
		}
	}
}