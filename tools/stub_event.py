import json
import os
import sys
import re

def extract_ink_assets(root_element):
    """Recursively scans the Ink JSON structure for raw text, choices, and reward IDs,
    handling arbitrary chained conditions (+ {c1} {c2} [Choice]) and ignoring
    string parameters inside evaluation blocks ("ev" ... "/ev").
    """
    text_lines = []
    choice_lines = []
    reward_lines = []
    if isinstance(root_element, list):
        choice_string_indices = set()

        # Pass 1: Identify choice labels and track their token indices
        for i, item in enumerate(root_element):
            # Detect choice descriptor: {"*": "...", "flg": ...}
            if isinstance(item, dict) and "*" in item and "flg" in item:
                # Scan backwards from the choice descriptor to find the immediate
                # "str" -> "^..." -> "/str" block that supplies the choice label.
                j = i - 1
                while j >= 2:
                    if (root_element[j] == "/ev" or root_element[j] == "/str") and j > 0:
                        # Check if we hit the closing /str of the choice label
                        str_end = j if root_element[j] == "/str" else (j - 1 if root_element[j - 1] == "/str" else None)
                        if str_end and str_end >= 2:
                            text_candidate = root_element[str_end - 1]
                            str_start = root_element[str_end - 2]

                            if (str_start == "str" and 
                                isinstance(text_candidate, str) and 
                                text_candidate.startswith("^")):
                                
                                clean_choice = text_candidate[1:].strip()
                                if clean_choice and clean_choice != "\n":
                                    if clean_choice != "Leave":
                                        choice_lines.append(clean_choice)
                                    choice_string_indices.add(str_end - 1)
                                break
                    j -= 1

        # Pass 2: Collect standard dialogue text and rewards, skipping eval blocks
        in_eval_mode = False
        for i, item in enumerate(root_element):
            if item == "ev":
                in_eval_mode = True
                continue
            elif item == "/ev":
                in_eval_mode = False
                continue

            # Skip choice label strings already processed
            if i in choice_string_indices:
                continue

            if isinstance(item, str) and item.startswith("^"):
                clean_text = item[1:].strip()
                if not clean_text or clean_text == "\n":
                    continue

                if ">>>" in clean_text:
                    match = re.search(r">>>(.+?):\s*(.+)", clean_text)
                    if match:
                        reward_lines.append(match.group(2).strip())
                elif not in_eval_mode:
                    # String parameters to functions (e.g. ^OnlySpells) appear while
                    # in_eval_mode is True and will be excluded here.
                    text_lines.append(clean_text)

            elif isinstance(item, (list, dict)):
                nested_text, nested_choice, nested_rewards = extract_ink_assets(item)
                text_lines.extend(nested_text)
                choice_lines.extend(nested_choice)
                reward_lines.extend(nested_rewards)

    elif isinstance(root_element, dict):
        # Modern choice format container: {"s": ["^ChoiceText", ...]}
        if "s" in root_element and isinstance(root_element["s"], list):
            for s_item in root_element["s"]:
                if isinstance(s_item, str) and s_item.startswith("^"):
                    clean_choice = s_item[1:].strip()
                    if clean_choice and clean_choice != "\n" and clean_choice != "Leave":
                        choice_lines.append(clean_choice)

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
    
    texts = sorted(list(set(texts)))
    choices = sorted(list(set(choices)))
    
    if not choices:
        print("Error: No choices found. Make sure the choice uses the \"[SingleWord]\" syntax.")
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
                "story_data": "data/"+filename,
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
        
    print(f"Successfully generated stub file: {output_filename}.\n" +
    "Add this file to your project and add the relative path to it in the AddMergedJsonFile call in your Plugin.cs Awake function.\n" + 
    "Do not add {filename} to the AddMergedJsonFile call, it is referenced by the generated file, add this file your project in the data/ directory.")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python stub_event.py <path_to_ink_json>")
    else:
        generate_story_event_stub(sys.argv[1])