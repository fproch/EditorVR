﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Menus
{
	public class MainMenuFace : MonoBehaviour
	{
		[SerializeField]
		private MeshRenderer m_BorderOutline;
		[SerializeField]
		private CanvasGroup m_CanvasGroup;
		[SerializeField]
		private Text m_FaceTitle;
		[SerializeField]
		private Transform m_GridTransform;
		[SerializeField]
		private SkinnedMeshRenderer m_TitleIcon;

		private enum RotationState
		{
			RotationBegin,
			RotationEnd
		}

		private enum VisualState
		{
			Hiding,
			Showing
		}

		private Material m_BorderOutlineMaterial;
		private Vector3 m_BorderOutlineOriginalLocalScale;
		private Transform m_BorderOutlineTransform;
		private List<Transform> m_MenuButtons;
		private RotationState m_RotationState;
		private Material m_TitleIconMaterial;
		private VisualState m_VisualState;

		private readonly float m_BorderScaleMultiplier = 1.0135f;
		private readonly string kBottomGradientProperty = "_ColorBottom";
		private readonly UnityBrandColorScheme.GradientPair kEmptyGradient = new UnityBrandColorScheme.GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);
		private readonly string kTopGradientProperty = "_ColorTop";

		private void Awake()
		{
			Assert.IsNotNull(m_BorderOutline, "m_BorderOutline is not assigned!");
			Assert.IsNotNull(m_CanvasGroup, "m_CanvasGroup is not assigned!");
			Assert.IsNotNull(m_FaceTitle, "m_FaceTitle is not assigned!");
			Assert.IsNotNull(m_GridTransform, "m_GridTransform is not assigned!");
			Assert.IsNotNull(m_TitleIcon, "m_TitleIcon is not assigned!");

			m_CanvasGroup.alpha = 0f;
			m_CanvasGroup.interactable = false;
			m_BorderOutlineMaterial = U.Material.GetMaterialClone(m_BorderOutline);
			m_BorderOutlineTransform = m_BorderOutline.transform;
			m_BorderOutlineOriginalLocalScale = m_BorderOutlineTransform.localScale;
			m_FaceTitle.text = "Not Set";
			m_TitleIconMaterial = U.Material.GetMaterialClone(m_TitleIcon);
			m_VisualState = VisualState.Hiding;

			SetGradientColors(kEmptyGradient);
		}

		public void SetFaceData(string faceName, List<Transform> buttons, UnityBrandColorScheme.GradientPair gradientPair)
		{
			if (m_MenuButtons != null && m_MenuButtons.Any())
				foreach (var button in m_MenuButtons)
					GameObject.DestroyImmediate(button);

			m_FaceTitle.text = faceName;
			m_MenuButtons = buttons;

			foreach (var button in buttons)
			{
				Transform buttonTransform = button.transform;
				buttonTransform.SetParent(m_GridTransform);
				buttonTransform.localRotation = Quaternion.identity;
				buttonTransform.localScale = Vector3.one;
				buttonTransform.localPosition = Vector3.zero;
			}

			SetGradientColors(gradientPair);
		}

		private void SetGradientColors(UnityBrandColorScheme.GradientPair gradientPair)
		{
			m_BorderOutlineMaterial.SetColor(kTopGradientProperty, gradientPair.ColorA);
			m_BorderOutlineMaterial.SetColor(kBottomGradientProperty, gradientPair.ColorB);
			m_TitleIconMaterial.SetColor(kTopGradientProperty, gradientPair.ColorA);
			m_TitleIconMaterial.SetColor(kBottomGradientProperty, gradientPair.ColorB);
		}

		public void Show()
		{
			m_BorderOutlineTransform.localScale = m_BorderOutlineOriginalLocalScale;
			StartCoroutine(AnimateShow());
		}

		public void Hide()
		{
			StartCoroutine(AnimateShow(VisualState.Hiding));
		}

		private IEnumerator AnimateShow(VisualState targetVisualState = VisualState.Showing)
		{
			m_CanvasGroup.interactable = false;
			m_VisualState = targetVisualState;

			float smoothTime = targetVisualState == VisualState.Showing ? 0.35f : 0.125f;
			float startingOpacity = m_CanvasGroup.alpha;
			float targetOpacity = targetVisualState == VisualState.Showing ? 1f : 0f;
			float smoothVelocity = 0f;
			while (m_VisualState == targetVisualState && !Mathf.Approximately(startingOpacity, targetOpacity))
			{
				startingOpacity = Mathf.SmoothDamp(startingOpacity, targetOpacity, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				m_CanvasGroup.alpha = startingOpacity * startingOpacity;
				yield return null;
			}

			if (m_VisualState == VisualState.Showing)
			{
				m_CanvasGroup.interactable = true;
				m_CanvasGroup.alpha = 1f;
			}
			else if (m_VisualState == VisualState.Hiding)
				m_TitleIcon.SetBlendShapeWeight(0, 0);
		}

		public void BeginRotationVisuals()
		{
			StartCoroutine(AnimateRotationVisuals(RotationState.RotationBegin));
		}

		public void EndRotationVisuals()
		{
			StartCoroutine(AnimateRotationVisuals(RotationState.RotationEnd));
		}

		private IEnumerator AnimateRotationVisuals(RotationState rotationState)
		{
			Vector3 targetBorderLocalScale = rotationState == RotationState.RotationBegin ? m_BorderOutlineOriginalLocalScale * m_BorderScaleMultiplier : m_BorderOutlineOriginalLocalScale;
			Vector3 currentBorderLocalScale = m_BorderOutlineTransform.localScale;

			m_RotationState = rotationState;
			float currentBlendShapeWeight = m_TitleIcon.GetBlendShapeWeight(0);
			float targetWeight = rotationState == RotationState.RotationBegin ? 100f : 0f;
			float smoothTime = rotationState == RotationState.RotationBegin ? 0.25f : 0.5f;
			const float kLerpEmphasisWeight = 0.2f;
			float smoothVelocity = 0f;
			while (m_RotationState == rotationState && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
			{
				currentBlendShapeWeight = Mathf.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentBorderLocalScale = Vector3.Lerp(currentBorderLocalScale, targetBorderLocalScale, currentBlendShapeWeight * kLerpEmphasisWeight);
				m_BorderOutlineTransform.localScale = currentBorderLocalScale;
				m_TitleIcon.SetBlendShapeWeight(0, currentBlendShapeWeight);
				yield return null;
			}

			if (m_RotationState == rotationState)
			{
				m_TitleIcon.SetBlendShapeWeight(0, targetWeight);
				m_BorderOutlineTransform.localScale = targetBorderLocalScale;
			}
		}
	}
}