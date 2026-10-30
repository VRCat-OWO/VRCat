import io

p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\App.xaml"
s = io.open(p, encoding="utf-8-sig").read()

anchor = '''    <Style x:Key="SegButton" TargetType="RadioButton">'''
assert anchor in s, "anchor missing"

style = '''    <!-- 重点模式开关：胶囊按钮，选中态由代码切换 Background / BorderBrush -->
    <Style x:Key="ChipToggle" TargetType="Button">
      <Setter Property="FontFamily" Value="{StaticResource UiFont}"/>
      <Setter Property="FontSize" Value="12.5"/>
      <Setter Property="FontWeight" Value="SemiBold"/>
      <Setter Property="Foreground" Value="{StaticResource Sub}"/>
      <Setter Property="Background" Value="#F2F5FA"/>
      <Setter Property="BorderBrush" Value="{StaticResource Line}"/>
      <Setter Property="Padding" Value="11,4"/>
      <Setter Property="Cursor" Value="Hand"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Border x:Name="bd" CornerRadius="9"
                    Background="{TemplateBinding Background}"
                    BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="1"
                    Padding="{TemplateBinding Padding}">
              <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="bd" Property="Background" Value="#FFF7D6"/>
              </Trigger>
              <Trigger Property="IsPressed" Value="True">
                <Setter TargetName="bd" Property="Background" Value="#FFEFA8"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

'''

s = s.replace(anchor, style + anchor, 1)
io.open(p, "w", encoding="utf-8", newline="").write(s)
print("ChipToggle style added")
