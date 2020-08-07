package application.util;

import org.apache.commons.codec.binary.Base64;

public class EncriptaDecriptaApacheCodec {
   
    public static String codificaBase64Encoder(String msg) {
        return new Base64().encodeToString(msg.getBytes());
    }

   
    public static String decodificaBase64Decoder(String msg) {
        return new String(new Base64().decode(msg));
    }


}
