#if UNITY_5_3_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

[assembly: InternalsVisibleTo("URID.Editor")]

namespace URID
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct Urid64
	{
		/// <summary>Serialized storage for Unity: the identifier as a number in bijective base 37.</summary>
		/// <remarks>
		/// <para>
		/// Unity writes a fixed buffer as a single hex string of its raw bytes: every word little-endian,
		/// least significant word first. The shared code never touches this buffer and works on the word fields
		/// that overlay it.
		/// </para>
		/// <para>
		/// The struct is not readonly because Unity's serializer skips readonly fields. The field name is part of
		/// the serialized asset format: renaming it breaks existing assets unless <c>[FormerlySerializedAs]</c> is added.
		/// </para>
		/// </remarks>
		[FieldOffset(0)]
		[SerializeField]
		private fixed ulong v[1];

		/// <summary>Word view of <see cref="v"/>, used by the shared code in <c>Urid.cs</c>.</summary>
		/// <remarks>Not serialized: private fields without <c>[SerializeField]</c> are skipped by Unity.</remarks>
		[FieldOffset(0)]
		private ulong v0;
	}

	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct Urid128
	{
		/// <inheritdoc cref="Urid64.v"/>
		[FieldOffset(0)]
		[SerializeField]
		private fixed ulong v[2];

		/// <summary>Word views of <see cref="v"/>, least significant first, used by the shared code in <c>Urid.cs</c>.</summary>
		/// <inheritdoc cref="Urid64.v0" path="/remarks"/>
		[FieldOffset(0)]
		private ulong v0;

		/// <inheritdoc cref="v0"/>
		[FieldOffset(8)]
		private ulong v1;
	}

	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct Urid192
	{
		/// <inheritdoc cref="Urid64.v"/>
		[FieldOffset(0)]
		[SerializeField]
		private fixed ulong v[3];

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(0)]
		private ulong v0;

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(8)]
		private ulong v1;

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(16)]
		private ulong v2;
	}

	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct Urid256
	{
		/// <inheritdoc cref="Urid64.v"/>
		[FieldOffset(0)]
		[SerializeField]
		private fixed ulong v[4];

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(0)]
		private ulong v0;

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(8)]
		private ulong v1;

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(16)]
		private ulong v2;

		/// <inheritdoc cref="Urid128.v0"/>
		[FieldOffset(24)]
		private ulong v3;
	}
}
#endif
