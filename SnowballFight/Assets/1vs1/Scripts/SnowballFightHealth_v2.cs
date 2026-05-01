using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnowballFightHealth_v2 : MonoBehaviour
{
    public float m_StartingHealth = 1f;                 // The amount of health each agent starts with.
    public Slider m_Slider;                             // The slider to represent how much health the agent currently has.
    public Image m_FillImage;                           // The image component of the slider.
    public Color m_FullHealthColor = Color.green;       // The color the health bar will be when on full health.
    public Color m_ZeroHealthColor = Color.red;         // The color the health bar will be when on no health.

    public float m_CurrentHealth;                      // How much health the agent currently has.
    public float m_NormalizedCurrentHealth;            // Normalized CurrentHealth

    public SnowballFightAgent_v2 m_Agent;               // The agent


    /// <summary>
    /// Set Health UI value
    /// </summary>
    public void SetHealth()
    {
        m_CurrentHealth = m_Agent.health;
       
        // Update the health slider's value and color.
        SetHealthUI();
    }

    private void SetHealthUI()
    {
        // Set the slider's value appropriately.
        m_Slider.value = m_CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
    }
}
