# Md_Extraction_test_nosummary

## Case 1

Description

``````yaml
expected:
  allowAdditionalProperties: false
  data: |
    # MdWithoutSummary

    ## Case 1

    Description

    ``````yaml
    db:
      FW_NS_FORM_WEB: 0
      FW_N_PUBL_FORM_WEB: 00000000-0000-0000-0000-000000000000
      FW_N_CONF: null
      FW_DE_CONT_FORM_WEB: null
      FW_NS_SYST_AUTR: 0
      FW_V_IDEN_UTIL: null
      FW_C_TYPE_FORM_WEB: null
      SubType: null
        Other: null
          Other: null
            Other: null
              Other: null
                Other: null
                  Other: null
                    Other: null
                      Other: null
                        Other: null
    expected:
      allowAdditionalProperties: false
      data: null
    ``````
``````