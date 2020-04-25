package gui.util;

import java.text.DecimalFormat;
import java.text.NumberFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

import application.domaim.CorpoDeProva;
import javafx.event.ActionEvent;
import javafx.scene.Node;
import javafx.scene.control.DatePicker;
import javafx.scene.control.MenuBar;
import javafx.scene.control.TableCell;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableRow;
import javafx.scene.control.TableView;
import javafx.stage.Stage;
import javafx.util.StringConverter;

public class Utils {

	private static NumberFormat format = NumberFormat.getInstance(new Locale("pt", "BR"));
	private static DecimalFormat df = (DecimalFormat)format;

	public static Stage currentStage(ActionEvent event) {
		return (Stage) ((Node) event.getSource()).getScene().getWindow();
	}

	public static Stage getMenuBarStage(MenuBar myMenuBar) {
		return (Stage) myMenuBar.getScene().getWindow();
	}

	public static Integer tryParseToInt(String str) {
		try {
			return Integer.parseInt(str);
		} catch (NumberFormatException e) {
			return null;
		}
	}

	public static Double tryParseToDouble(String str) {
		try {
			Number number = format.parse(str);
			return number.doubleValue();
		} catch (NumberFormatException | ParseException e) {
			return null;
		}
	}

	public static void formatDatePicker(DatePicker datePicker, String format) {
		datePicker.setConverter(new StringConverter<LocalDate>() {
			DateTimeFormatter dateFormatter = DateTimeFormatter.ofPattern(format);
			{
				datePicker.setPromptText(format.toLowerCase());
			}

			@Override
			public String toString(LocalDate date) {
				if (date != null) {
					return dateFormatter.format(date);
				} else {
					return "";
				}
			}

			@Override
			public LocalDate fromString(String string) {
				if (string != null && !string.isEmpty()) {
					return LocalDate.parse(string, dateFormatter);
				} else {
					return null;
				}
			}
		});
	}

	public static <T> void formatTableColumnDate(TableColumn<T, Date> tableColumn, String format) {
		tableColumn.setCellFactory(column -> {
			TableCell<T, Date> cell = new TableCell<T, Date>() {
				SimpleDateFormat sdf = new SimpleDateFormat(format);

				@Override
				protected void updateItem(Date item, boolean empty) {
					super.updateItem(item, empty);
					if (empty) {
						setText(null);
					} else {
						sdf.setTimeZone(TimeZone.getDefault());
						setText(sdf.format(item));
					}
				}
			};
			return cell;
		});
	}

	public static <T> void formatTableColumnDouble(TableColumn<T, Double> tableColumn, int decimalPlaces) {
		tableColumn.setCellFactory(column -> {
			TableCell<T, Double> cell = new TableCell<T, Double>() {
				@Override
				protected void updateItem(Double item, boolean empty) {
					super.updateItem(item, empty);
					if (empty) {
						setText(null);
					} else {
						String strDecimalPlaces = ".";
						for (int i=1; i <= decimalPlaces; i++) {
							strDecimalPlaces = strDecimalPlaces + "0";
						}
						df.applyPattern("#,###" + strDecimalPlaces);
						if (item != null) {
							setText(df.format(item));
						}
					}
				}
			};
			return cell;
		});
	}
	
	public static String doubleFormat(Double number) {
		df.applyPattern("#,###.00");
		return df.format(number);
	}
	
	public static void formatCorpoDeProvaTableViewRowColor(TableView<CorpoDeProva> tableView) {
		tableView.setRowFactory(row -> new TableRow<CorpoDeProva> (){
			@Override
			public void updateItem(CorpoDeProva item, boolean empty) {
				super.updateItem(item,empty);
				
				if (item == null || empty) {
					setStyle("");
				} else {
					if (item.getRuptureDate().compareTo(new Date()) < 0 && (item.getTonRupture() == null || item.getTonRupture() == 0f)) {
						setStyle("-fx-background-color: red");
					} 
				}
			}
		});
	}
	
	
	public static Instant getInsTantFromDatePicker(DatePicker dP) {
		Instant instant = Instant.from(dP.getValue().atStartOfDay(ZoneId.systemDefault()));
		return instant;
	}
}
