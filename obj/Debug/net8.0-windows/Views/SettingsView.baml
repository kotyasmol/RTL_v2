   M S B A M L   `   `   `  ����  TFortisBoard    RTL.Views.SettingsView   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      S OHandyControl, Version=3.5.2.0, Culture=neutral, PublicKeyToken=45be8712787a1e5b0hc'https://handyorg.github.io/handycontrol  5      .��5      �� $
��Auto=�6   $
��Auto=�66   .��5   
   �� $��10q�6   .+�5	      9� $#��Общие настройки��6     9� 
IsExpanded$
  True��60   .��5
      �� $��10q�6   .+�5      �� $��Выбор темы:��6    $
��Bold"�63   �� 5      $	��200��6e   $
��Left=�6q   ��6   �� *Themes+ �� SelectedItem 6>   �� *SelectedTheme+6q   �� 5      $#��Папка для логов:��6    $
��Bold"�68   $��0,10,0,0q�6J   �� 5       �� Orientation$ 
Horizontal=�6!   .+�5      �� $	��200��6N    �� 
IsReadOnly$
 True��6Z   ��6"   �� *LogFolderPath+ �� Mode$ TwoWay=�6Z   �� 5      $��Выбрать...��6!   $��5,0,0,0q�6a   ��66   �� *SelectLogFolderCommand+6a   5      5      5      9� 5      $��Настройки RTL-SW��6   $  False��61   .��5      �� $��10q�6   .+�5      �� $��Профиль RTL-SW:��6    $
��Bold"�67   �� 5      $ 
Horizontal=�6!   .+�5      �� $	��200��6Q   $
 True��6]   ��6"   �� *RtlSwProfilePath+$ TwoWay=�6]   �� 5      $��Выбрать...��6!   $��5,0,0,0q�6d   ��66   �� *SelectRtlSwProfileCommand+6d   5      �� 5      $��	COM (SW):��6    $
��Bold"�61   $��0,10,0,0q�6C   �� 5        �� 
IsEditable$
 True��6|   $	��200��6�   $
��Left=�6�   ��6   �� *AvailablePorts+ 6F   �� *SelectedComPort+$ TwoWay=�6�   �� 5"      $��	DUT (SW):��6    $
��Bold"�61   $��0,10,0,0q�6C   �� 5#      $ 
Horizontal=�6!   .+�5$      �� $	��200��5%      $
��Left=�6   ��5$   "   �� *SelectedDutPort+$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�5%      �� 5&      $��Обновить��6!   $��5,0,0,0q�6\   ��64   �� *RefreshPortsCommand+6\   5'      �� 5*      $)��#FLASH Прошивка (Xgpro.exe):��6    $
��Bold"�6C   $��0,10,0,0q�6U   �� 5+      $ 
Horizontal=�6!   .+�5,      �� $	��200��6Q   $
 True��6]   ��6"   �� *FlashProgramPath+$ TwoWay=�6]   �� 5-      $��Выбрать...��6!   $��5,0,0,0q�6d   ��66   �� *SelectFlashProgramCommand+6d   5.      5/      50      9� 53      $ ��Настройки RTL-POE��6   $  False��62   .��54      �� $��10q�6   .+�55      �� $��Профиль RTL-POE:��6    $
��Bold"�68   �� 56      $ 
Horizontal=�6!   .+�57      �� $	��200��6R   $
 True��6^   ��6"   �� *RtlPoeProfilePath+$ TwoWay=�6^   �� 58      $��Выбрать...��6!   $��5,0,0,0q�6e   ��66   �� *SelectRtlPoeProfileCommand+6e   59      �� 5;      $��
COM (POE):��6    $
��Bold"�62   $��0,10,0,0q�6D   �� 5<      $
 True��5>      $	��200��5?      $
��Left=�5@      ��5<      �� *AvailablePorts+ 5=      �� *SelectedComPortPOE+$ TwoWay=�5@      5A      5B      5D      5E      5F      