package application.test;

import application.util.EncriptaDecriptaApacheCodec;

public class Test {
	public static void main(String[] args) {

		String helloWorld = "Hello world";
		String coded =EncriptaDecriptaApacheCodec.codificaBase64Encoder(helloWorld);
		System.out.println(coded );
		System.out.println(EncriptaDecriptaApacheCodec.decodificaBase64Decoder(coded));
	}

}
