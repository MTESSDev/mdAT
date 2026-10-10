# Test_object_deserialization

> Test object deserialization 

## Case 1

Description

``````yaml
expected:
  name: cas1
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: |
        # Md2
    
        ## Case 1
    
        Description
    
        ``````yaml
        formList:
          Value: 
            - FW_NS_FORM_WEB: 0
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
                              Other: null
                              Obj: null
                            Obj: null
                          Obj: null
                        Obj: null
                      Obj: null
                    Obj: null
                  Obj: null
                Obj: null
          Exception: 
            ClassName: null
            Message: null
            ParamName: null
            ObjectName: null
        ``````
``````