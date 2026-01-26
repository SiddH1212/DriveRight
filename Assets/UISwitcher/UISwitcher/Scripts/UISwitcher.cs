using UnityEngine;
using UnityEngine.UI;

namespace UISwitcher
{
    public class UISwitcher : UINullableToggle
    {
        private readonly Vector2 _min = new(0, 0.5f);
        private readonly Vector2 _max = new(1, 0.5f);
        private readonly Vector2 _middle = new(0.5f, 0.5f);

        [Header("Visuals")]
        [SerializeField] private Graphic backgroundGraphic;
        [SerializeField] private Color onColor, offColor, nullColor;
        [SerializeField] private RectTransform tipRect;

        [Header("Binding")]
        [SerializeField] private CarController car;

        private bool updatingFromCar;

        private Color backgroundColor
        {
            set
            {
                if (backgroundGraphic != null)
                    backgroundGraphic.color = value;
            }
        }

        /* ===================== UNITY ===================== */
		private void Awake()
		{
			if (car == null)
				car = FindFirstObjectByType<CarController>();
		}
        private void OnEnable()
        {
            if (car != null)
                car.OnGearChanged += HandleGearChangedFromCar;
        }

        private void OnDisable()
        {
            if (car != null)
                car.OnGearChanged -= HandleGearChangedFromCar;
        }

        /* ===================== CAR → UI ===================== */

        private void HandleGearChangedFromCar(bool isReverse)
        {
            updatingFromCar = true;

            if (isReverse)
                SetOn();
            else
                SetOff();

            updatingFromCar = false;
        }

        /* ===================== UI → CAR ===================== */

        protected override void OnChanged(bool? value)
        {
            // Always update visuals
            if (value.HasValue)
            {
                if (value.Value)
                    SetOn();
                else
                    SetOff();
            }
            else
            {
                SetNull();
            }

            // Prevent feedback loop
            if (updatingFromCar)
                return;

            // User interaction drives the car
            if (value.HasValue && car != null)
                car.SetReverseFromUIToggle(value.Value);
        }

        /* ===================== VISUALS ===================== */

        private void SetOn()
        {
            SetAnchors(_max);
            backgroundColor = onColor;
        }

        private void SetOff()
        {
            SetAnchors(_min);
            backgroundColor = offColor;
        }

        private void SetNull()
        {
            SetAnchors(_middle);
            backgroundColor = nullColor;
        }

        private void SetAnchors(Vector2 anchor)
        {
            tipRect.anchorMin = anchor;
            tipRect.anchorMax = anchor;
            tipRect.pivot = anchor;
        }
    }
}
