<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";
*/
//remote machine
$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB


$contenthabits = "SELECT
  contents.ContentBody,
  contents.ContentTitle,
  contents.Content_ID,
  contents.ContentType,
  content_habitparameter.Habit_ID,
  content_habitparameter.Min_Value,
  content_habitparameter.Max_Value,
  content_habitparameter.yesorno,
  content_habitparameter.Section_Generic,
  habits.label,
  habits.description
FROM content_habitparameter
  RIGHT OUTER JOIN contents
    ON content_habitparameter.Content_ID = contents.Content_ID
  LEFT OUTER JOIN habits
    ON content_habitparameter.Habit_ID = habits.Habit_ID";
    echo $contenthabits;
$result = mysqli_query($conn, $contenthabits) or die("2:name check query failed");

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

?>
<!DOCTYPE html>
<html>
<head>
	<link rel="stylesheet" href="mystyle.css">
	<title></title>
</head>
<body >
<table border="1"> 

	<tr>
    <td>
      Content Title
    </td>

      <td>
        Description
      </td>
            <td>
        Habit Title
      </td>
            <td>
        Habit Question
      </td>
            <td>
        Min Value
      </td>
            <td>
        Max Value
      </td>
            <td>
        Yes/No
      </td>
            <td>
        Startup/Everywhere
      </td>

</tr>
<tr>
    <?php foreach($dbdata as $item): 
        echo "
        <tr><td>
        <p1><a href='newhabitsforcontent.php?contentid=" . $item['Content_ID'] . "'>" . $item['ContentTitle'] . 
        "</a></td><td>
        <p1>" . $item['ContentBody'] . 
        "</td><td>
        <p1>" . $item['label'] . 
        "</td><td>
        <p1>". $item['description'] . 
        "</td><td>
        <p1>". $item['Min_Value'] . 
        "</td><td>
        <p1>". $item['Max_Value'] . 
        "</td><td>
        <p1>". $item['yesorno'] . 
        "</td><td>
        <p1>". $item['Section_Generic']; ?>
        	</p></td><td> 
    	<?php endforeach; ?>

</tr>

</table>
</form>
<a href="home.html">Home</a>
</body>
</html>