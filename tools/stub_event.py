"""
Python script to generate a story event data definition from a compiled ink script (.ink.json)
"Usage: python stub_event.py <path_to_ink_json>"
"""
import json
import os
import sys
import re

def extract_ink_assets(root_element):
    """Recursively scans the Ink JSON structure for raw text and choices."""
    text_lines = []
    choice_lines = []
    reward_lines = []

    if isinstance(root_element, list):
        # Scan for legacy flat choice sequence format (ev -> str -> ^Text -> /str -> /ev -> choiceObj)
        i = 0
        while i < len(root_element):
            if (i + 5 < len(root_element) and 
                root_element[i] == "ev" and 
                root_element[i+1] == "str" and 
                isinstance(root_element[i+2], str) and root_element[i+2].startswith("^") and
                root_element[i+3] == "/str" and 
                root_element[i+4] == "/ev" and 
                isinstance(root_element[i+5], dict) and "*" in root_element[i+5]):
                
                raw_choice = root_element[i+2][1:].strip()
                if raw_choice and raw_choice != "\n" and raw_choice != "Leave":
                    choice_lines.append(raw_choice)
                i += 6
                continue
            
            item = root_element[i]
            if isinstance(item, str) and item.startswith("^"):
                clean_text = item[1:].strip()
                if clean_text and clean_text != "\n" and ">>>" not in clean_text:
                    text_lines.append(clean_text)
                elif clean_text and clean_text != "\n" and ">>>" in clean_text:
                    match =re.search(">>>(.+):\s(.+)", clean_text)
                    reward_lines.append(match[2])
            elif isinstance(item, (list, dict)):
                nested_text, nested_choice, nested_rewards = extract_ink_assets(item)
                text_lines.extend(nested_text)
                choice_lines.extend(nested_choice)
                reward_lines.extend(nested_rewards)
            i += 1

    elif isinstance(root_element, dict):
        # Scan modern choice array wrappers if they slip in
        if "s" in root_element and isinstance(root_element["s"], list):
            for s_item in root_element["s"]:
                if isinstance(s_item, str) and s_item.startswith("^"):
                    clean_choice = s_item[1:].strip()
                    if clean_choice and clean_choice != "\n" and clean_choice != "Leave":
                        choice_lines.append(clean_choice)
        
        # Scan all other dictionary attributes
        for key, value in root_element.items():
            if key != "s":
                nested_text, nested_choice, nested_rewards = extract_ink_assets(value)
                text_lines.extend(nested_text)
                choice_lines.extend(nested_choice)
                reward_lines.extend(nested_rewards)

    return text_lines, choice_lines, reward_lines

def generate_story_event_stub(json_file_path):
    if not os.path.exists(json_file_path):
        print(f"Error: File not found at {json_file_path}")
        return

    filename = os.path.basename(json_file_path)
    
    with open(json_file_path, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)

    root_array = data.get("root", [])
    if not root_array or len(root_array) == 0:
        print("Error: Empty or invalid root array structure.")
        return

    knot_name = "<UNKNOWN_KNOT>"
    knot_bytecode = None

    # Fix: Instead of filtering for '#f', grab the dictionary structure at the tail end
    # inklecate 0.9.0 outputs the main named knot dictionary block as the final structural element.
    last_element = root_array[-1]
    
    if isinstance(last_element, dict):
        for key, value in last_element.items():
            # Filter out any internal formatting flags or empty properties if present
            if not key.startswith("#") and isinstance(value, list):
                knot_name = key
                knot_bytecode = value
                break

    if not knot_bytecode:
        print("Error: Could not locate the named knot dictionary at the end of the root array structure.")
        return

    # Extract clean text and choice entries
    texts, choices, rewards = extract_ink_assets(knot_bytecode)
    
    texts = list(set(texts))
    
    if not choices:
        print("Error: No choices found. Make sure the choice uses the \"+ [SingleWord]\" syntax.")
        return

    # Reconstruct inside your exact template layout schema
    template = {
        "$schema": "https://github.com/Monster-Train-2-Modding-Group/Trainworks-Reloaded/releases/latest/download/schema.json",
        "events": [
            {
                "id": knot_name,
                "knot_name": knot_name,
                "num_runs_completed_to_see": 1,
                "priority_ticket_count": 10,
                "num_classes_needed_to_show": 1,
                "min_distance_allowed": 3,
                "max_distance_allowed": 8,
                "story_data": filename,
                "is_followup_event": False,
                "texts": [{"english": line} for line in texts],
                "choice_texts": [
                    {
                        "choice": choice,
                        "texts": {
                            "english": "<The actual choice text goes here.>"
                        },
                        "preview_obtain_texts": {
                            "english": "Get {0}.",
                            "french":  "Obtenez {0}.",
                            "german":  "Belohnung: {0}.",
                            "russian": "Получите: «{0}».",
                            "portuguese": "Receba {0}.",
                            "chinese": "获得{0}。",
                            "spanish": "Obtienes {0}.",
                            "chinese_traditional": "獲得{0}。",
                            "korean": "{0}를 획득합니다.",
                            "japanese": "{0}を得る。"
                        },
                        "preview_infos": [
                            {
                                "preview_type": "<insert type here>",
                                "references": ["@<reference of the above type this is usually not the reward>"]
                            }
                        ]
                    } for choice in choices
                ]
            }
        ],
        "rewards": [
            {
              "id": reward,
              "type": "<insert type here>",
              "is_story_reward": True,
              "costs": [ 100 ],
              "extensions": [
                {
                    "<insert type here>": {
                    }
                }
              ]
            } for reward in rewards
        ]
    }

    output_filename = f"event_{knot_name}.json"
    
    with open(output_filename, 'w', encoding='utf-8') as f:
        json.dump(template, f, indent=4, ensure_ascii=False)
        
    print(f"Successfully generated stub file: {output_filename}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python stub_event.py <path_to_ink_json>")
    else:
        generate_story_event_stub(sys.argv[1])