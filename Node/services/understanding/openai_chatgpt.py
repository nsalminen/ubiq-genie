import sys
import os
import argparse
import base64
from openai import OpenAI

def save_image(image_data):
    # Convert the byte array string to an actual byte array
    image_data = bytes(int(b) for b in image_data.strip(b',').split(b','))
    file_path = os.path.join("received_image.png")
    with open(file_path, "wb") as f:
        f.write(image_data)
    return file_path

def encode_image_to_base64(image_path):
    with open(image_path, "rb") as image_file:
        img_base64 = base64.b64encode(image_file.read()).decode('utf-8')
    return f"data:image/png;base64,{img_base64}"

def request_response(image_path=None):
    global client

    if image_path:
        img_str = encode_image_to_base64(image_path)

        messages = [
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": "Any text message"},
                    {"type": "image_url", "image_url": {"url": img_str}}
                ],
            }
        ]

        response = client.chat.completions.create(
            model="gpt-4o",
            messages=messages,
            max_tokens=300
        )

        return response.choices[0].message
    else:
        return "No image data received."

def listen_for_messages(args):
    while True:
        try:
            line = sys.stdin.buffer.readline()
            if len(line) == 0 or line.isspace():
                continue

            image_data = line.strip()  # The received image data in byte array format

            image_path = save_image(image_data)
            response_content = request_response(image_path)
            print(response_content)
            print(">" + response_content.content)
        except KeyboardInterrupt:
            break

if __name__ == "__main__":
    global client
    client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

    parser = argparse.ArgumentParser()
    parser.add_argument("--preprompt", type=str, default="")
    parser.add_argument("--prompt_suffix", type=str, default="")
    args = parser.parse_args()

    listen_for_messages(args)
