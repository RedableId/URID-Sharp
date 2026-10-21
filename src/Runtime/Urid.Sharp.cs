#if !UNITY_5_3_OR_NEWER
namespace URID
{
	public readonly partial struct Urid64
	{
		/// <summary>The identifier as a number in bijective base 37; storage for plain .NET.</summary>
		/// <remarks>
		/// All storage fields of a Urid are declared in this platform part only:
		/// C# leaves the field order undefined when fields are spread over several parts of a partial struct.
		/// </remarks>
		private readonly ulong _v0;
	}

	public readonly partial struct Urid128
	{
		/// <summary>The identifier as a number in bijective base 37, least significant word first; storage for plain .NET.</summary>
		/// <inheritdoc cref="Urid64._v0" path="/remarks"/>
		private readonly ulong _v0, _v1;
	}

	public readonly partial struct Urid192
	{
		/// <inheritdoc cref="Urid128._v0"/>
		private readonly ulong _v0, _v1, _v2;
	}

	public readonly partial struct Urid256
	{
		/// <inheritdoc cref="Urid128._v0"/>
		private readonly ulong _v0, _v1, _v2, _v3;
	}
}
#endif
