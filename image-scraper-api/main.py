import os
import shutil
import re
import time
import zipfile
from datetime import datetime


import pandas as pd
import requests
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse


app = FastAPI(title="Image Scraper API")


app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def scrape_image_from_duckduckgo(query):
    try:
        headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
        search_res = requests.get("https://duckduckgo.com/", params={"q": query}, headers=headers)
        match = re.search(r'vqd=([\d-]+)\&', search_res.text)
        if not match: return None
        token = match.group(1)
        params = {"l": "us-en", "o": "json", "q": query, "vqd": token, "f": ",,,", "p": "1"}
        img_res = requests.get("https://duckduckgo.com/i.js", params=params, headers=headers).json()
        if not img_res.get("results"): return None
        return img_res["results"][0]["image"]
    except Exception as e:
        return None


@app.post("/api/scrape")
async def scrape_excel_file(
    file: UploadFile = File(...),
    column_name: str = Form("DESCRIPTION")
):
    timestamp = int(datetime.now().timestamp())
    upload_dir = "temp_uploads"
    session_dir = os.path.join(upload_dir, f"session_{timestamp}")
    os.makedirs(session_dir, exist_ok=True)

    file_location = os.path.join(session_dir, file.filename)
    with open(file_location, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    try:
        df = pd.read_excel(file_location)
    except Exception as e:
        return JSONResponse(status_code=400, content={"message": f"Error reading Excel: {e}"})

    if column_name not in df.columns:
        return JSONResponse(status_code=400, content={"message": f"Column '{column_name}' not found. Available: {df.columns.tolist()}"})

    downloaded_count = 0
    images_folder = os.path.join(session_dir, "images")
    os.makedirs(images_folder, exist_ok=True)

    for index, row in df.iterrows():
        item_name = row.get(column_name, '')
        if pd.notna(item_name) and str(item_name).strip() != "":
            img_url = scrape_image_from_duckduckgo(str(item_name))
            if img_url:
                try:
                    img_data = requests.get(img_url, headers={"User-Agent": "Mozilla/5.0"}, timeout=10).content
                    safe_name = "".join(c for c in str(item_name) if c.isalnum() or c in (' ', '_')).strip()
                    img_filename = f"{index}_{safe_name[:30]}.jpg"
                    img_path = os.path.join(images_folder, img_filename)
                    with open(img_path, "wb") as f:
                        f.write(img_data)
                    downloaded_count += 1
                except Exception as e:
                    pass
            time.sleep(0.5)

    zip_filename = f"scraped_images_{timestamp}.zip"
    zip_path = os.path.join(session_dir, zip_filename)

    with zipfile.ZipFile(zip_path, 'w') as zipf:
        for root, _, files in os.walk(images_folder):
            for file in files:
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, images_folder)
                zipf.write(file_path, arcname)

    print(f"Finished. Downloaded {downloaded_count} images.")
    return FileResponse(path=zip_path, media_type="application/zip", filename=zip_filename)

if __name__ == "__main__":
    import uvicorn
    print("Starting Server on http://127.0.0.1:8000")
    uvicorn.run(app, host="127.0.0.1", port=8000)