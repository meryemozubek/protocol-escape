# Protocol: Escape | 2D Top-Down Asset Paketi

Orijinal, Unity için hazırlanmış 32 px tabanlı pixel-art assetleri.

## Dosyalar
- characters/engineer/ ve characters/robot/: 4 yön x 3 hareket karesi, ayrı PNG (32x40).
- characters/*_spritesheet_3x4.png: sütunlar hareket kareleri (3), satırlar down/up/left/right (4). 96x160 px.
- tiles/floor_*.png: 3 adet 32x32 açık gri zemin.
- tiles/wall_*.png: 32x32 koyu gri duvar, kenar, köşe varyantları.
- tiles/lava_*.png: 6x2 parçadan oluşan geniş lav alanı (12 adet 32x32).
- tiles/lava_tileset_6x2.png: 192x64 px, 6 sütun 2 satır.
- objects/water_1x1.png: 32x32 tek su bloğu.
- objects/button_unpressed.png ve button_pressed.png: 32x32.
- doors/door_frame.png: 32x32 sabit kapı çerçevesi.
- doors/door_closed.png, door_sliding_75/50/25.png, door_open.png: 32x32 sürgü durumları.
- previews/: gösterim için büyütülmüş önizlemeler, oyunda kullanılacak sprite değildir.

## Unity içe aktarımı
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single (ayrı PNG) / Multiple (spritesheet)
- Pixels Per Unit: 32 (karakter 32x40 olduğundan 1 x 1.25 Unity birimi).
- Filter Mode: Point (no filter); Compression: None; Generate Mip Maps: Off.
- Karakter spritesheet: 32x40 Grid by Cell Size; 3 sütun, 4 satır.
- Lav spritesheet: 32x32 Grid by Cell Size; 6 sütun, 2 satır.
- Zemin, duvar, lav, su için Tilemap ve Tile Palette kullan.
- Duvar Collider2D kullanır. Lav hasar ve su iyileştirme mantığı mevcut kodlarda tutulmalı.
- Kapı ileri açılmaz veya dönmez, duvar içine YATAY SOLA kayar. Açılınca geçiş collider kapatılmalı.
- Obje isimleri ve mevcut oyun mantığına ait script/yorum satırları korunmalı.

## Codex'e verilecek görev
Bu asset paketini mevcut Unity projesine import et. Mevcut sahne, kapı/buton/sağlık/RL mekaniklerini bozmadan uygun sprite referanslarını yerleştir. Import ayarlarını yukarıdaki gibi uygula. Tilemap veya sprite object ayrımını mevcut sahne hiyerarşisine göre yap. Kapı yatay olarak duvar içine kayarak açılsın; mevcut kapı mantığını koru. Animasyon controller kullanacaksan 4 yön x 3 kareyi ayır. Scriptlerdeki mevcut yorum satırlarını silme ya da değiştirme. Yeni sahne/objeleri kontrol et, yapılan değişikliklerin kısa özetini ver.
