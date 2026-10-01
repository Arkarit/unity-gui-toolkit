using System;

namespace GuiToolkit
{
	/// <summary>
	/// Non-generic base of <see cref="PerCameraSetting{T}"/>, for lists and the property drawer.
	/// </summary>
	[Serializable]
	public abstract class PerCameraSettingBase
	{
		/// <summary>Only enabled settings are applied; all others leave the global value untouched.</summary>
		public bool Enabled;

		public abstract bool IsApplied { get; }

		internal abstract void Apply();
		internal abstract void Restore();

		/// <summary>Copy the current global value into this setting.</summary>
		public abstract void Capture();
	}

	/// <summary>
	/// One override of a global (static) value, e.g. <c>RenderSettings.fogColor</c>, for the duration
	/// of one camera's rendering. Holds whether it is used, the value to set and the saved original.
	///
	/// The global is accessed through a typed getter/setter pair that the owning component binds before use,
	/// so there is neither reflection nor boxing. Bind with non-capturing lambdas, e.g.
	/// <c>Bind(() => RenderSettings.fog, v => RenderSettings.fog = v)</c>: the compiler caches those,
	/// so binding allocates nothing.
	/// </summary>
	[Serializable]
	public class PerCameraSetting<T> : PerCameraSettingBase
	{
		public T Value;

		[NonSerialized] private T m_saved;
		[NonSerialized] private bool m_isApplied;
		[NonSerialized] private Func<T> m_getter;
		[NonSerialized] private Action<T> m_setter;

		public PerCameraSetting() { }

		public PerCameraSetting( T _value, bool _enabled = false )
		{
			Value = _value;
			Enabled = _enabled;
		}

		public override bool IsApplied => m_isApplied;
		public bool IsBound => m_getter != null && m_setter != null;

		public PerCameraSetting<T> Bind( Func<T> _getter, Action<T> _setter )
		{
			m_getter = _getter;
			m_setter = _setter;
			return this;
		}

		internal override void Apply()
		{
			if (!Enabled || m_isApplied || !IsBound)
				return;

			m_saved = m_getter();
			m_setter(Value);
			m_isApplied = true;
		}

		internal override void Restore()
		{
			if (!m_isApplied)
				return;

			m_setter(m_saved);
			m_saved = default;   // don't keep object references alive
			m_isApplied = false;
		}

		public override void Capture()
		{
			if (m_getter != null)
				Value = m_getter();
		}
	}
}
