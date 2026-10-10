# External_file2

> External file include 2

## Case 1

Description

``````yaml
form: !include ext_form.yml
expected:
  verify:
  - type: match
    allowAdditionalProperties: false
    data: |
      FW_NS_FORM_WEB: 123
      FW_N_PUBL_FORM_WEB: 00000000-0000-0000-0000-000000000000
      FW_N_CONF: null
      FW_DE_CONT_FORM_WEB: null
      FW_NS_SYST_AUTR: 0
      FW_V_IDEN_UTIL: null
      FW_C_TYPE_FORM_WEB: null
      SubType: 
        Other: 
          Other: 
            Other: null
            Obj: '123'
``````