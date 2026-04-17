#!/usr/bin/env python3
import re

file_path = r"C:\Users\Hoang Long\Source\Repos\FoodStreetGuide_28_3\Services\Localization\AppResources.cs"

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Vietnamese
content = content.replace(
  '{ "TapToListen2", "?? Nh?n ?? nghe bình lu?n" },\n}',
    '{ "TapToListen2", "?? Nh?n ?? nghe bình lu?n" },\n        { "TabMap", "B?n ??" },\n        { "TabPoi", "?i?m" },\n        { "TabQr", "QR" },\n    { "TabSettings", "Cài ??t" },\n         }'
)

# English
content = content.replace(
    '{ "TapToListen2", "?? Tap to listen commentary" },\n    }\n},',
    '{ "TapToListen2", "?? Tap to listen commentary" },\n    { "TabMap", "Map" },\n    { "TabPoi", "Points" },\n    { "TabQr", "QR" },\n    { "TabSettings", "Settings" },\n    }\n},'
)

# French
content = content.replace(
 '{ "TapToListen2", "?? Appuyez pour écouter le commentaire" },\n  }\n          },',
    '{ "TapToListen2", "?? Appuyez pour écouter le commentaire" },\n     { "TabMap", "Carte" },\n{ "TabPoi", "Points" },\n    { "TabQr", "QR" },\n{ "TabSettings", "Paramètres" },\n }\n          },'
)

# Chinese (Simplified)
content = content.replace(
    '{ "TapToListen2", "?? ?????" },\n   }\n },',
    '{ "TapToListen2", "?? ?????" },\n   { "TabMap", "??" },\n    { "TabPoi", "??" },\n    { "TabQr", "???" },\n    { "TabSettings", "??" },\n   }\n },'
)

# Japanese
content = content.replace(
    '{ "TapToListen2", "?? ??????????" },\n   }\n },',
    '{ "TapToListen2", "?? ??????????" },\n   { "TabMap", "??" },\n { "TabPoi", "??" },\n    { "TabQr", "QR" },\n    { "TabSettings", "??" },\n   }\n },'
)

# Korean
content = content.replace(
    '{ "TapToListen2", "?? ??? ???? ???" },\n   }\n        },',
    '{ "TapToListen2", "?? ??? ???? ???" },\n   { "TabMap", "??" },\n    { "TabPoi", "??" },\n    { "TabQr", "QR" },\n { "TabSettings", "??" },\n   }\n        },'
)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("TabBar keys added successfully!")
