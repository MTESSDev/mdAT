# External_file2

> External file include 2

## Case 1

Description

``````yaml
form: !include ext_form.yml
expected:
  allowAdditionalProperties: false
  data: |
    "FW_NS_FORM_WEB: 123\r\nFW_N_PUBL_FORM_WEB: 00000000-0000-0000-0000-000000000000\r\nFW_N_CONF: null\r\nFW_DE_CONT_FORM_WEB: null\r\nFW_NS_SYST_AUTR: 0\r\nFW_V_IDEN_UTIL: null\r\nFW_C_TYPE_FORM_WEB: null\r\nSubType: \r\n  Other: \r\n    Other: \r\n      Other: null\r\n      Obj: 123"
``````