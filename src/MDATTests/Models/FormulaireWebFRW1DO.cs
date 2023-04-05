namespace MDATTests.Models;

/// <summary>
/// Form obj
/// </summary>
public class FormulaireWebFRW1DO
{
    /// <summary>
    /// Numéro séquentiel de formulaire
    /// </summary>
    public int FW_NS_FORM_WEB { get; set; }

    /// <summary>
    /// Numéro public de formulaire (GUID)
    /// </summary>
    public Guid FW_N_PUBL_FORM_WEB { get; set; }

    /// <summary>
    /// Numéro de confirmation de transmission (optionnel)
    /// </summary>
    public long? FW_N_CONF { get; set; }

    /// <summary>
    /// Contenu du formulaire JSON, compressé
    /// </summary>
    public string FW_DE_CONT_FORM_WEB { get; set; } = default!;

    /// <summary>
    /// [FK] Numéro de système autorisé
    /// </summary>
    public int FW_NS_SYST_AUTR { get; set; }

    /// <summary>
    /// Code utilisateur ou identifiant de ressource assignée
    /// </summary>
    public string? FW_V_IDEN_UTIL { get; set; }

    /// <summary>
    /// Type de formulaire
    /// </summary>
    /// <example>3003</example>
    public string FW_C_TYPE_FORM_WEB { get; set; } = default!;

    /// <summary>
    /// Sub type test
    /// </summary>
    public SubType SubType { get; set; }

}