import json

def append_examples():
    md_path = r"C:\Users\JoseTrinidad\.gemini\antigravity\brain\4a8a8e31-39f7-4e47-8570-51579a06a752\dgii_ecf_requirements.md"
    json_path = r"C:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Services\requiredByType.json"
    
    with open(json_path, 'r', encoding='utf-8') as f:
        examples = json.load(f)
        
    with open(md_path, 'a', encoding='utf-8') as f:
        f.write("\n\n---\n\n## 4. Ejemplos Prácticos de Payload JSON\n\n")
        f.write("A continuación se presentan ejemplos básicos y funcionales del objeto JSON a enviar a la plataforma por cada tipo de comprobante principal, ilustrando cómo completar las propiedades descritas anteriormente.\n\n")
        
        for item in examples:
            if "ECF" in item and "Encabezado" in item["ECF"] and "IdDoc" in item["ECF"]["Encabezado"]:
                tipo = item["ECF"]["Encabezado"]["IdDoc"].get("TipoeCF")
                notas = item.get("NOTAS", "")
                
                f.write(f"### TipoeCF: {tipo}\n")
                if notas:
                    f.write(f"> [!NOTE]\n> {notas}\n\n")
                    
                # We strip out the CamposRequeridosObligatorios so the example is clean
                if "CamposRequeridosObligatorios" in item:
                    del item["CamposRequeridosObligatorios"]
                if "NOTAS" in item:
                    del item["NOTAS"]
                if "Notas" in item:
                    del item["Notas"]
                    
                json_str = json.dumps(item, indent=2, ensure_ascii=False)
                f.write("```json\n" + json_str + "\n```\n\n")

if __name__ == '__main__':
    append_examples()
