﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class RoslynExtensions {
		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = ImmutableHashSet.Create(
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.UTF8Encoding",
			"System.IO.Abstractions.IFileSystem"
		);

		/// <summary>
		/// Get the declaration syntax for a symbol. This is intended to be
		/// used for fields and properties which can't have multiple
		/// declaration nodes.
		/// </summary>
		public static T GetDeclarationSyntax<T>( this ISymbol symbol )
			where T : SyntaxNode {
			ImmutableArray<SyntaxReference> decls = symbol.DeclaringSyntaxReferences;

			if( decls.Length != 1 ) {
				throw new NotImplementedException(
					"Unexepected number of decls: "
					+ decls.Length
				);
			}

			SyntaxNode syntax = decls[0].GetSyntax();

			var decl = syntax as T;
			if( decl == null ) {

				string msg = String.Format(
						"Couldn't cast declaration syntax of type '{0}' as type '{1}': {2}",
						syntax.GetType().FullName,
						typeof( T ).FullName,
						symbol.ToDisplayString()
					);

				throw new InvalidOperationException( msg );
			}

			return decl;
		}

		public static bool IsFromOtherAssembly( this ITypeSymbol type, Compilation compilation ) {
			return type.ContainingAssembly != compilation.Assembly;
		}

		public static ImmutabilityScope GetImmutabilityScope( this ITypeSymbol type ) {
			if( type.IsTypeMarkedImmutable() ) {
				return ImmutabilityScope.SelfAndChildren;
			}

			if( type.IsTypeMarkedImmutableBaseClass() ) {
				return ImmutabilityScope.Self;
			}

			return ImmutabilityScope.None;
		}

		private static bool IsTypeMarkedImmutable( this ITypeSymbol symbol ) {
			if( symbol.IsExternallyOwnedMarkedImmutableType() ) {
				return true;
			}
			if( symbol.IsMarkedImmutableGeneric() ) {
				return true;
			}
			if( Attributes.Objects.Immutable.IsDefined( symbol ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedImmutable ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedImmutable( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}

		private static bool IsTypeMarkedImmutableBaseClass( this ITypeSymbol symbol ) {
			if( Attributes.Objects.ImmutableBaseClass.IsDefined( symbol ) ) {
				return true;
			}
			return false;
		}

		public static bool IsExternallyOwnedMarkedImmutableType( this ITypeSymbol symbol ) {
			return MarkedImmutableTypes.Contains( symbol.GetFullTypeName() );
		}

		private static bool IsMarkedImmutableGeneric( this ITypeSymbol symbol ) {
			AttributeData ignore;
			return TryGetImmutableGenericAnnotation( symbol, out ignore );
		}

		private static bool TryGetImmutableGenericAnnotation( this ITypeSymbol symbol, out AttributeData attribute ) {
			attribute = null;
			var type = symbol as INamedTypeSymbol;
			if( type == null ) {
				return false;
			}

			if( !type.IsGenericType ) {
				return false;
			}

			/*  We can have an annotation in:
			 *  (1) symbol's assembly,
			 *  (2) any of symbol's type arguments's assemblies
			 */
			if( type.ContainingAssembly.TryGetImmutableGenericAnnotation( type, out attribute ) ) {
				return true;
			}

			foreach( var typeArgument in type.TypeArguments ) {
				// Can happen with generics, but I don't fully understand when
				// it does, exactly.
				if( typeArgument.ContainingAssembly == null ) {
					continue;
				}

				if( typeArgument.ContainingAssembly.TryGetImmutableGenericAnnotation( type, out attribute ) ) {
					return true;
				}
			}
			attribute = null;
			return false;
		}

		private static bool TryGetImmutableGenericAnnotation( this IAssemblySymbol assembly, ITypeSymbol type, out AttributeData attribute ) {
			attribute = null;

			var attributes = Attributes.Objects.ImmutableGeneric.GetAll( assembly );
			foreach( var attr in attributes ) {

				if( attr.ConstructorArguments.Length != 1 ) {
					continue;
				}

				var arg = attr.ConstructorArguments[ 0 ];
				//Using ToString, otherwise it sometimes fails to match, and the test behaviour does not match the real behaviour
				if( arg.Value.ToString().Equals( type.ToString() ) ) {
					attribute = attr;
					return true;
				}
			}

			return false;
		}

		internal static IEnumerable<AttributeData> GetAllImmutableAttributesApplied( this ITypeSymbol type ) {
			var immutable = Attributes.Objects.Immutable.GetAll( type ).FirstOrDefault();
			if( immutable != null ) {
				yield return immutable;
			}

			var immutableBaseClass = Attributes.Objects.ImmutableBaseClass.GetAll( type ).FirstOrDefault();
			if( immutableBaseClass != null ) {
				yield return immutableBaseClass;
			}

			AttributeData immutableGeneric;
			if( TryGetImmutableGenericAnnotation( type, out immutableGeneric ) ) {
				yield return immutableGeneric;
			}
		}


		public static bool IsTypeMarkedSingleton( this ITypeSymbol symbol ) {
			if( Attributes.Singleton.IsDefined( symbol ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedSingleton ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedSingleton( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}

		private static readonly SymbolDisplayFormat FullTypeDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		private static readonly SymbolDisplayFormat FullTypeWithGenericsDisplayFormat = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
		);

		public static string GetFullTypeName( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeDisplayFormat );
			return fullyQualifiedName;
		}


		public static string GetFullTypeNameWithGenericArguments( this ITypeSymbol symbol ) {
			var fullyQualifiedName = symbol.ToDisplayString( FullTypeWithGenericsDisplayFormat );
			return fullyQualifiedName;
		}

		public static IEnumerable<ISymbol> GetExplicitNonStaticMembers( this ITypeSymbol type ) {
			return type.GetMembers()
				.Where( t => !t.IsStatic && !t.IsImplicitlyDeclared );
		}

		public static bool IsNullOrErrorType( this ITypeSymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}
			if( symbol.TypeKind == TypeKind.Error ) {
				return true;
			}

			return false;
		}

		public static bool IsNullOrErrorType( this ISymbol symbol ) {
			if( symbol == null ) {
				return true;
			}
			if( symbol.Kind == SymbolKind.ErrorType ) {
				return true;
			}

			return false;
		}

		public static bool IsGenericType( this ISymbol symbol ) {

			if( symbol is INamedTypeSymbol namedType ) {
				return ( namedType.TypeParameters.Length > 0 );
			}

			return false;
		}

		/// <summary>
		/// Find the matching type argument in the base interface
		/// that corresponds to this type parameter.  That is,
		/// if we have Foo<S, T>: IFoo<S>, IBar<T>, this will
		/// match up the Foo S, to the IFoo S, but will get -1
		/// from IBar since it doesn't have S.
		/// </summary>
		public static int IndexOfArgument(
			this INamedTypeSymbol intf,
			string name
		) {

			for( int ordinal = 0; ordinal < intf.TypeArguments.Length; ordinal++ ) {
				if( string.Equals( intf.TypeArguments[ ordinal ].Name, name, StringComparison.Ordinal ) ) {
					return ordinal;
				}
			}

			return -1;
		}
	}
}
