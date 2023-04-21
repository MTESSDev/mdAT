# Exception_test

> Test Exception

## Case 1

Description

``````yaml
form:
  FW_NS_FORM_WEB: 0
  FW_N_PUBL_FORM_WEB: 00000000-0000-0000-0000-000000000000
  FW_N_CONF: null
  FW_DE_CONT_FORM_WEB: null
  FW_NS_SYST_AUTR: 0
  FW_V_IDEN_UTIL: null
  FW_C_TYPE_FORM_WEB: null
  SubType: 
    Other: 
      Other: 
        Other: 
          Other: 
            Other: 
              Other: 
                Other: 
                  Other: 
                    Other: null
                    Obj: null
                  Obj: null
                Obj: null
              Obj: null
            Obj: null
          Obj: null
        Obj: null
      Obj: null
    Obj: null
expected:
  allowAdditionalProperties: true
  generateExpectedData: null
  data: 
    ClassName: System.InvalidOperationException
    Message: Operation is not valid due to the current state of the object.
``````