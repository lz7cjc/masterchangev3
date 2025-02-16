<!DOCTYPE html>
<html>
<body>

<?php
$habits = array (
  array(29,0,1),
  array(30,1,27),
  array(31,0,0),
  array(33,0,1)
);



foreach ( $habits as habits ) {

  echo '<dl style="margin-bottom: 1em;">';

  foreach ( $habits as $key => $value ) {
    echo "<dt>$key</dt><dd>$value</dd>";
  }

  echo '</dl>';

}

?>

</body>
</html>
