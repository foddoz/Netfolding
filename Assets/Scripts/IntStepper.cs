using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntStepper : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Button increaseButton;
    public Button decreaseButton;

    public int minValue = 0;
    public int maxValue = 10;

    public int displayOffset = 1;

    private int _value = 0;
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Mathf.Clamp(value, minValue, maxValue);
            if (_value == clamped) return;

            _value = clamped;
            UpdateUI();
            onValueChanged?.Invoke(_value);
        }
    }


    public System.Action<int> onValueChanged;

    void Start()
    {
        increaseButton.onClick.AddListener(() => ChangeValue(1));
        decreaseButton.onClick.AddListener(() => ChangeValue(-1));
        UpdateUI();
    }

    void ChangeValue(int delta)
    {
        int range = maxValue - minValue + 1;
        int newValue = ((_value + delta - minValue + range) % range) + minValue;

        Value = newValue;
    }


    public void SetValueWrapped(int value)
    {
        int range = maxValue + 1;
        Value = ((value % range) + range) % range; // wraps negative too
    }

    void UpdateUI()
    {
        if (valueText != null)
            valueText.text = (_value + displayOffset).ToString();
    }
}
