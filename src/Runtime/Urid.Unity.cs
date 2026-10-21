#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;

namespace URID
{
	[Serializable]
	public partial struct Urid64
	{
		/// <summary>The identifier as a number in bijective base 37; storage for Unity.</summary>
		/// <remarks>
		/// <para>
		/// The struct is not readonly because Unity's serializer skips readonly fields.
		/// The field names are part of the serialized asset format: renaming them breaks existing assets
		/// unless <c>[FormerlySerializedAs]</c> is added.
		/// </para>
		/// <para>
		/// All storage fields of a Urid are declared in this platform part only:
		/// C# leaves the field order undefined when fields are spread over several parts of a partial struct.
		/// </para>
		/// </remarks>
		[SerializeField]
		private ulong _v0;
	}

	[Serializable]
	public partial struct Urid128
	{
		/// <summary>The identifier as a number in bijective base 37, least significant word first; storage for Unity.</summary>
		/// <inheritdoc cref="Urid64._v0" path="/remarks"/>
		[SerializeField]
		private ulong _v0, _v1;
	}

	[Serializable]
	public partial struct Urid192
	{
		/// <inheritdoc cref="Urid128._v0"/>
		[SerializeField]
		private ulong _v0, _v1, _v2;
	}

	[Serializable]
	public partial struct Urid256
	{
		/// <inheritdoc cref="Urid128._v0"/>
		[SerializeField]
		private ulong _v0, _v1, _v2, _v3;
	}
}
#endif
