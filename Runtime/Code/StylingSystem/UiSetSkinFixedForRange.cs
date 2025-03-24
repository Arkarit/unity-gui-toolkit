using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Style
{
	/// <summary>
	/// This component is intended to suppress skin changes in a range of UiAbstractApplyStyleBase instances.
	/// It can be handy if you want varying skins in one screen, but a fixed skin in another.
	///
	/// In case of nested UISetSkinFixedForRange, the outer one does not collect the children of the inner one.
	/// </summary>
	public class UiSetSkinFixedForRange : MonoBehaviour
	{
		[Tooltip("Set a specific UiStyleConfig. If left 'none', the global UiMainStyleConfig is used. Note that ALL appliers need to also have set the same config!")]
		[SerializeField] private UiStyleConfig m_optionalStyleConfig;
		[Tooltip("Check this if you want a fixed list of style appliers to be handled. If unchecked, all objects in the child hierarchy are handled")]
		[SerializeField] private bool m_applierListFixed;
		[Tooltip("Check this if you want dynamically added and removed style appliers to be handled")]
		[SerializeField] private bool m_handleDynamicallyAddedAppliers = true;
		[SerializeField] private List<UiAbstractApplyStyleBase> m_styleAppliers = new();
		[FormerlySerializedAs("m_FixedSkinName")] [SerializeField] private string m_fixedSkinName;
		
		private readonly Dictionary<UiAbstractApplyStyleBase, string> m_savedSkinSettings = new ();
		private bool m_isActive;
		
		public UiStyleConfig StyleConfig => m_optionalStyleConfig != null ? m_optionalStyleConfig : UiMainStyleConfig.Instance;

		public List<UiAbstractApplyStyleBase> StyleAppliers
		{
			get => m_styleAppliers;
			set
			{
				if (m_isActive)
				{
					RestoreStyleAppliers();
					m_styleAppliers = value;
					SaveStyleAppliersAndSetFixed();
					return;
				}
				
				m_styleAppliers = value;
			}
		}
		
		public string FixedSkinName
		{
			get => m_fixedSkinName;
			set
			{
				if (m_fixedSkinName == value)
					return;
				
				if (m_isActive)
				{
					RestoreStyleAppliers();
					m_fixedSkinName = value;
					SaveStyleAppliersAndSetFixed();
					return;
				}
				
				m_fixedSkinName = value;
			}
		}
		
		private void Awake()
		{
			if (!m_applierListFixed)
			{
				m_styleAppliers.Clear();
				FindStyleAppliersRecursive(transform, m_styleAppliers);			
			}
		}

		private void OnEnable()
		{
			m_isActive = true;
			SaveStyleAppliersAndSetFixed();
			if (!m_handleDynamicallyAddedAppliers)
				return;
			
			UiEventDefinitions.EvStyleApplierCreated.AddListener(OnStyleApplierCreated);
			UiEventDefinitions.EvStyleApplierChangedParent.AddListener(OnStyleApplierChangedParent);
			UiEventDefinitions.EvStyleApplierDestroyed.AddListener(OnStyleApplierDestroyed);
		}

		private void OnDisable()
		{
			m_isActive = false;
			RestoreStyleAppliers();
			if (!m_handleDynamicallyAddedAppliers)
				return;

			UiEventDefinitions.EvStyleApplierCreated.RemoveListener(OnStyleApplierCreated);
			UiEventDefinitions.EvStyleApplierChangedParent.RemoveListener(OnStyleApplierChangedParent);
			UiEventDefinitions.EvStyleApplierDestroyed.RemoveListener(OnStyleApplierDestroyed);
		}

		private void OnStyleApplierCreated(UiAbstractApplyStyleBase _applier)
		{
			if (!IsMyselfOrMyDescendant(_applier.transform))
				return;
			
			AddStyleApplier(_applier);
		}
		
		private void OnStyleApplierDestroyed(UiAbstractApplyStyleBase _applier)
		{
			if (m_styleAppliers.Contains(_applier))
			{
				// We needn't restore anything, since the applier is destroyed anyways
				m_styleAppliers.Remove(_applier);
				m_savedSkinSettings.Remove(_applier);
			}
		}

		private void OnStyleApplierChangedParent(UiAbstractApplyStyleBase _applier)
		{
			if (m_styleAppliers.Contains(_applier))
			{
				// nothing changed?
				if (IsMyselfOrMyDescendant(_applier.transform))
					return;
				
				// now no descendant anymore, remove it
				RemoveStyleApplier(_applier);
				return;
			}
			
			if (!IsMyselfOrMyDescendant(_applier.transform))
				return;
			
			AddStyleApplier(_applier);
		}

		private bool IsMyselfOrMyDescendant(Transform _potentialChild)
		{
			if (_potentialChild == null)
				return false;
			
			for (Transform current = _potentialChild; current != null; current = current.parent)
			{
				if (current == transform)
					return true;
				
				if (current.GetComponent<UiSetSkinFixedForRange>() != null)
					return false;
			}
			
			return false;
		}
		
		private void FindStyleAppliersRecursive(Transform _tf, List<UiAbstractApplyStyleBase> _styleAppliers)
		{
			var styleAppliers = _tf.GetComponents<UiAbstractApplyStyleBase>();
			if (styleAppliers.Length > 0)
				foreach (var styleApplier in styleAppliers)
					_styleAppliers.Add(styleApplier);
			
			foreach (Transform child in _tf)
			{
				if (child.GetComponent<UiSetSkinFixedForRange>() != null)
					continue;
				
				FindStyleAppliersRecursive(child, _styleAppliers);
			}
		}
		
		private void AddStyleApplier(UiAbstractApplyStyleBase _styleApplier)
		{
			if (m_styleAppliers.Contains(_styleApplier))
				return;
			
			m_styleAppliers.Add(_styleApplier);
			SaveStyleApplierAndSetFixed(_styleApplier);
		}
		
		private void RemoveStyleApplier(UiAbstractApplyStyleBase _styleApplier)
		{
			if (!m_styleAppliers.Contains(_styleApplier))
				return;
			
			m_styleAppliers.Remove(_styleApplier);
			RestoreStyleApplier(_styleApplier);
		}
		
		private void SaveStyleAppliersAndSetFixed()
		{
			foreach (var styleApplier in m_styleAppliers)
				SaveStyleApplierAndSetFixed(styleApplier);
		}

		private void RestoreStyleAppliers()
		{
			foreach (var kv in m_savedSkinSettings)
			{
				var applier = kv.Key;
				if (applier == null)
					continue;
				
				var name = kv.Value;
				applier.FixedSkinName = name;
			}

			m_savedSkinSettings.Clear();
		}
		
		private void SaveStyleApplierAndSetFixed(UiAbstractApplyStyleBase _styleApplier)
		{
			if (_styleApplier == null)
				return;
			
			if (!m_savedSkinSettings.TryAdd(_styleApplier, _styleApplier.FixedSkinName))
				return;

			_styleApplier.FixedSkinName = m_fixedSkinName;
		}

		private void RestoreStyleApplier(UiAbstractApplyStyleBase _styleApplier)
		{
			if (_styleApplier == null)
				return;
			
			if (!m_savedSkinSettings.TryGetValue(_styleApplier, out string name))
			    return;
			    
			_styleApplier.FixedSkinName = name;
			m_savedSkinSettings.Remove(_styleApplier);
		}
	}
}
