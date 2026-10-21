using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace URID.Editor
{
	/// <summary>
	/// Inspector drawer that shows a Urid as text instead of its raw serialized words.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An edit is committed on Enter or when the field loses focus, and the field then shows the stored value:
	/// lowercased, with other characters turned into the separator and cut to the type's maximum length.
	/// The tooltip names the type and its maximum length unless the field has its own <c>[Tooltip]</c>.
	/// </para>
	/// <para>
	/// While the field is being edited, the input border turns yellow when at most <see cref="AlmostFullThreshold"/>
	/// symbols are left and red when the text no longer fits, so the cut on commit does not come as a surprise.
	/// The border is cleared once the edit is committed or the field loses focus.
	/// </para>
	/// <para>
	/// The drawer reads and writes the serialized fixed buffer <c>v</c> through <see cref="SerializedProperty"/>,
	/// so undo and multi-object editing work as for any built-in field, and the text follows changes made elsewhere,
	/// such as an undo.
	/// </para>
	/// <para>
	/// Only the UI Toolkit path is implemented: in an IMGUI-based custom editor Unity shows its
	/// "No GUI implemented" placeholder for Urid fields.
	/// </para>
	/// </remarks>
	[CustomPropertyDrawer(typeof(Urid64))]
	[CustomPropertyDrawer(typeof(Urid128))]
	[CustomPropertyDrawer(typeof(Urid192))]
	[CustomPropertyDrawer(typeof(Urid256))]
	internal sealed class UridDrawer : PropertyDrawer
	{
		/// <summary>Name of the serialized fixed buffer in <c>Urid.Unity.cs</c>.</summary>
		private const string WordsField = "v";

		/// <summary>Number of remaining symbols at which the input border turns yellow.</summary>
		private const int AlmostFullThreshold = 3;

		private static readonly Color AlmostFullColor = new Color(0.96f, 0.74f, 0.1f);
		private static readonly Color OverflowColor = new Color(0.87f, 0.22f, 0.22f);

		/// <inheritdoc/>
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			SerializedProperty words = property.FindPropertyRelative(WordsField);
			if (words == null || !words.isFixedBuffer)
				return new Label($"{property.displayName}: unsupported Urid layout");

			int maxLength = MaxLength(words.fixedBufferSize);
			var field = new TextField(property.displayName)
			{
				isDelayed = true,
				tooltip = string.IsNullOrEmpty(property.tooltip)
					? $"{property.type}, up to {maxLength} symbols"
					: property.tooltip,
			};
			field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
			VisualElement input = field.Q(TextField.textInputUssName);
			Refresh(field, words);

			field.RegisterCallback<FocusInEvent>(_ =>
			{
				if (!field.showMixedValue)
					UpdateBorder(input, field.text.Length, maxLength);
			});
			// A delayed field reports ChangeEvent only on commit; InputEvent follows every keystroke.
			field.RegisterCallback<InputEvent>(e => UpdateBorder(input, e.newData.Length, maxLength));
			field.RegisterCallback<FocusOutEvent>(_ => SetBorderColor(input, StyleKeyword.Null));
			field.RegisterValueChangedCallback(e =>
			{
				Write(words, e.newValue);
				words.serializedObject.ApplyModifiedProperties();
				Refresh(field, words);
				SetBorderColor(input, StyleKeyword.Null);
			});
			field.TrackPropertyValue(property, _ => Refresh(field, words));

			return field;
		}

		/// <summary>Shows the stored value, or the mixed-value placeholder when the selected objects differ.</summary>
		private static void Refresh(TextField field, SerializedProperty words)
		{
			field.showMixedValue = HasMixedValues(words);
			field.SetValueWithoutNotify(Read(words));
		}

		/// <summary>Colors the input border by how many of the <paramref name="maxLength"/> symbols are left.</summary>
		private static void UpdateBorder(VisualElement input, int length, int maxLength)
		{
			if (length > maxLength)
				SetBorderColor(input, OverflowColor);
			else if (maxLength - length <= AlmostFullThreshold)
				SetBorderColor(input, AlmostFullColor);
			else
				SetBorderColor(input, StyleKeyword.Null);
		}

		/// <summary>Sets all four border colors; <see cref="StyleKeyword.Null"/> restores the theme's own colors.</summary>
		private static void SetBorderColor(VisualElement input, StyleColor color)
		{
			input.style.borderTopColor = color;
			input.style.borderBottomColor = color;
			input.style.borderLeftColor = color;
			input.style.borderRightColor = color;
		}

		/// <summary>Decodes the identifier stored in the serialized words.</summary>
		internal static string Read(SerializedProperty words)
		{
			switch (words.fixedBufferSize)
			{
				case 1: return new Urid64(Get(words, 0)).ToString();
				case 2: return new Urid128(Get(words, 0), Get(words, 1)).ToString();
				case 3: return new Urid192(Get(words, 0), Get(words, 1), Get(words, 2)).ToString();
				case 4: return new Urid256(Get(words, 0), Get(words, 1), Get(words, 2), Get(words, 3)).ToString();
				default: throw new ArgumentOutOfRangeException(nameof(words), words.fixedBufferSize, "Unexpected Urid size.");
			}
		}

		/// <summary>Parses <paramref name="text"/> and stores the result in the serialized words.</summary>
		/// <remarks>The caller applies the change with <see cref="SerializedObject.ApplyModifiedProperties"/>.</remarks>
		internal static void Write(SerializedProperty words, string text)
		{
			ReadOnlySpan<char> src = text.AsSpan();
			switch (words.fixedBufferSize)
			{
				case 1:
				{
					var v = new Urid64(src).Vec;
					Set(words, 0, v.V0);
					break;
				}
				case 2:
				{
					var v = new Urid128(src).Vec;
					Set(words, 0, v.V0);
					Set(words, 1, v.V1);
					break;
				}
				case 3:
				{
					var v = new Urid192(src).Vec;
					Set(words, 0, v.V0);
					Set(words, 1, v.V1);
					Set(words, 2, v.V2);
					break;
				}
				case 4:
				{
					var v = new Urid256(src).Vec;
					Set(words, 0, v.V0);
					Set(words, 1, v.V1);
					Set(words, 2, v.V2);
					Set(words, 3, v.V3);
					break;
				}
				default: throw new ArgumentOutOfRangeException(nameof(words), words.fixedBufferSize, "Unexpected Urid size.");
			}
		}

		private static ulong Get(SerializedProperty words, int index) => words.GetFixedBufferElementAtIndex(index).ulongValue;

		private static void Set(SerializedProperty words, int index, ulong value) => words.GetFixedBufferElementAtIndex(index).ulongValue = value;

		private static bool HasMixedValues(SerializedProperty words)
		{
			for (int i = 0; i < words.fixedBufferSize; i++)
			{
				if (words.GetFixedBufferElementAtIndex(i).hasMultipleDifferentValues)
					return true;
			}
			return false;
		}

		private static int MaxLength(int words)
		{
			switch (words)
			{
				case 1: return Urid64.MaxLength;
				case 2: return Urid128.MaxLength;
				case 3: return Urid192.MaxLength;
				default: return Urid256.MaxLength;
			}
		}
	}
}
