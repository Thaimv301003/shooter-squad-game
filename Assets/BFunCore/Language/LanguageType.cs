// File: LanguageType.cs
// Purpose: Defines all supported languages, categorized by market priority.
namespace BFunCoreKit
{
    public enum LanguageType
    {
        // --- Tier 1: "Must-Have" Markets ---
        English,            // Global default language.
        SimplifiedChinese,  // China - Largest player base (PC/Mobile).
        Japanese,           // Japan - Core console and story-driven game market.
        German,             // Germany - Largest European market, strong on PC.
        French,             // France, Canada, etc. - Core European market with global reach.
        Spanish,            // Spain & Latin America - Massive native speaker base.
        Korean,             // South Korea - Esports & Online PC gaming hub.

        // --- Tier 2: High Potential Markets ---
        BrazilianPortuguese,// Brazil - Largest Latin American market.
        Russian,            // Russia & Eastern Europe - Massive PC (Steam) player base.
        Polish,             // Poland - Rapidly growing and engaged Eastern European market.

        // --- Tier 3: Strategic & Growing Markets ---
        Italian,            // Italy - Stable European market.
        Turkish,            // Turkey - Large, young Steam community.
        Arabic,             // Middle East & North Africa - High potential, but requires technical support for Right-to-Left (RTL) UI.

        // --- Tier 3: Southeast Asia (Mobile Boom) ---
        Thai,               // Thailand
        Vietnamese,         // Vietnam
        Indonesian          // Indonesia
    }
}