using UnityEngine;
using TMPro;

public sealed class BeatInputUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController _player;

    [Header("Slots (A/T/_/X)")]
    [SerializeField] private TMP_Text _beat1;
    [SerializeField] private TMP_Text _beat2;
    [SerializeField] private TMP_Text _beat3;

    private void LateUpdate()
    {
        if (_player == null || _beat1 == null || _beat2 == null || _beat3 == null)
        {
            return;
        }

        _player.GetCurrentMeasureBeatSymbols(out char b1, out char b2, out char b3);
        SetSlot(_beat1, b1);
        SetSlot(_beat2, b2);
        SetSlot(_beat3, b3);
    }

    private static void SetSlot(TMP_Text t, char symbol)
    {
        if (t == null)
        {
            return;
        }

        string s = symbol switch
        {
            'A' => "A",
            'T' => "T",
            'X' => "X",
            _ => "_"
        };

        if (t.text != s)
        {
            t.text = s;
        }
    }
}

