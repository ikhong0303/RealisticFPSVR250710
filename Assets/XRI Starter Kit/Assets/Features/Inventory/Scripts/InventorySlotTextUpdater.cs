using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class InventorySlotTextUpdater : MonoBehaviour
    {
        public TextMeshProUGUI currentCount;
        public TextMeshProUGUI maxCount;

        private InventorySlot inventorySlot;

        void Awake()
        {
            inventorySlot = inventorySlot ?? GetComponent<InventorySlot>();
            inventorySlot.onSlotUpdated += CheckTypes;
        }

        private void CheckTypes(XRBaseInteractable currentSlotItem)
        {
            if (!currentSlotItem)
            {
                HideText();
                return;
            }

            // ProjectileWeapon이면 무조건 ∞ 표시
            if (currentSlotItem.GetComponent<ProjectileWeapon>() != null)
                SetTextToInfinity();
            else
                HideText();
        }

        private void HideText()
        {
            currentCount.text = "";
            maxCount.text = "";
        }

        private void SetTextToInfinity()
        {
            currentCount.text = "";
            maxCount.text = "∞";
        }
    }
}